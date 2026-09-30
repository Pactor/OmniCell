r"""Index the client's named message bus.

Utils.dll exports Message::Find<T>(const char* key, T* out, int) and the
matching Add<T>(const char* key, T value, int). Every signal that crosses
between the game code and the interface travels as one of these, so the
key strings are the client's own names for fields that carry no name at
all on the wire.

This walks a module's .text, remembers recent `push imm32` operands, and
reports the key whenever one of those calls is reached. Read-only.

    python GuiKeys.py "E:\Funcom\Anarchy Online\GUI.dll"
    python GuiKeys.py <image> --near 0x10106553 --span 0x200
    python GuiKeys.py <image> --key Sender
"""

from __future__ import annotations

import argparse
import re
from pathlib import Path

import capstone
import pefile

ACCESSOR = re.compile(r'\?(Find|Add|Set|Get)([A-Za-z0-9_]*)@(Message|Variant)@@')
PRINTABLE = re.compile(rb'^[ -~]{1,64}\x00')


def accessors(pe, image_base):
    """IAT slot -> short accessor name, for the Message/Variant entry points."""
    pe.parse_data_directories(
        directories=[pefile.DIRECTORY_ENTRY['IMAGE_DIRECTORY_ENTRY_IMPORT']])
    found = {}
    for library in getattr(pe, 'DIRECTORY_ENTRY_IMPORT', []):
        for imported in library.imports:
            if not imported.name:
                continue
            name = imported.name.decode('ascii', errors='replace')
            match = ACCESSOR.search(name)
            if match:
                found[imported.address] = match.group(1) + match.group(2)
    return found


def string_at(pe, image_base, data_by_section, address):
    for start, end, blob in data_by_section:
        if start <= address < end:
            offset = address - start
            match = PRINTABLE.match(blob[offset:offset + 65])
            if match:
                return match.group()[:-1].decode('ascii')
    return None


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument('image', type=Path)
    parser.add_argument('--near', type=lambda v: int(v, 0))
    parser.add_argument('--span', type=lambda v: int(v, 0), default=0x400)
    parser.add_argument('--key')
    parser.add_argument('--window', type=int, default=6,
                        help='how many instructions back a push may sit')
    args = parser.parse_args()

    pe = pefile.PE(str(args.image), fast_load=True)
    image_base = pe.OPTIONAL_HEADER.ImageBase
    slots = accessors(pe, image_base)
    if not slots:
        raise SystemExit('no Message/Variant accessors imported by this module')

    sections = [(image_base + s.VirtualAddress,
                 image_base + s.VirtualAddress + len(s.get_data()),
                 s.get_data()) for s in pe.sections]
    text = next(s for s in pe.sections if s.Name.rstrip(b'\0') == b'.text')
    blob = text.get_data()
    text_va = image_base + text.VirtualAddress

    lo = args.near - args.span if args.near else text_va
    hi = args.near + args.span if args.near else text_va + len(blob)
    start = max(0, lo - text_va)
    stop = min(len(blob), hi - text_va)

    disassembler = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    disassembler.detail = True
    disassembler.skipdata = True

    pushes: list[tuple[int, int]] = []
    cached: dict[int, int] = {}
    for instruction in disassembler.disasm(blob[start:stop], text_va + start):
        if instruction.mnemonic == 'mov':
            operands = instruction.operands
            # MSVC hoists a hot IAT slot into a register and calls it many
            # times over; without this the loop bodies look like bare calls.
            if (len(operands) == 2
                    and operands[0].type == capstone.x86.X86_OP_REG
                    and operands[1].type == capstone.x86.X86_OP_MEM
                    and operands[1].mem.base == 0):
                target = operands[1].mem.disp & 0xFFFFFFFF
                if target in slots:
                    cached[operands[0].reg] = target
                else:
                    cached.pop(operands[0].reg, None)
            elif operands and operands[0].type == capstone.x86.X86_OP_REG:
                cached.pop(operands[0].reg, None)
        if instruction.mnemonic == 'push':
            operands = instruction.operands
            if operands and operands[0].type == capstone.x86.X86_OP_IMM:
                pushes.append((instruction.address, operands[0].imm))
                pushes = pushes[-args.window:]
            continue
        if instruction.mnemonic != 'call':
            if instruction.mnemonic in ('ret', 'jmp'):
                pushes = []
            continue
        operands = instruction.operands
        slot = None
        for operand in operands:
            if operand.type == capstone.x86.X86_OP_MEM and operand.mem.base == 0:
                slot = operand.mem.disp & 0xFFFFFFFF
            elif operand.type == capstone.x86.X86_OP_REG:
                slot = cached.get(operand.reg)
        if slot is None or slot not in slots:
            pushes = []
            continue
        key = None
        for _, value in reversed(pushes):
            key = string_at(pe, image_base, sections, value & 0xFFFFFFFF)
            if key:
                break
        pushes = []
        if key is None:
            continue
        if args.key and args.key.lower() not in key.lower():
            continue
        print('0x%08X  %-18s %r' % (instruction.address, slots[slot], key))


if __name__ == '__main__':
    main()
