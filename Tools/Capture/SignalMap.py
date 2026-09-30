r"""Map the client's global signals to the code that consumes them.

A message dispatcher usually ends by emitting one of the signals hanging
off `GlobalSignals_c::GetInstance()`, and for a long time that was a dead
end: nothing in Gamecode subscribes. The subscribers live in GUI.dll, and
they are what give the fields names - a list handler puts each field into
a named column, a button handler builds a request out of one of them.

Every subscription looks the same: call the exported GetInstance, add the
signal's offset, and hand `connect` the object and a member-function
pointer. This finds them and prints the offset with the handler address.

Reaching GetInstance has three shapes and only two of them were matched until
2026-09-12: a direct `call [import]`, a call to its jmp thunk, and - the one
that was missing - a `mov ebx, [import]` once followed by `call ebx` for each
signal. A window that connects several handlers always takes the third, so the
misses were concentrated where the answers are. GUI.dll's mail window connects
its inbox handler that way at 0x10109EF6, and this tool reported signal + 0x274
as having no subscriber anywhere. Two pages had leaned on that kind of silence.
The costly one was OrgServer: its kind 9 emits + 0x24C, the page said nothing
subscribes, and in fact GUI.dll connects a handler at 0x1005345E that asks
"Are you sure you want to transfer organization leadership to %s?" - which is
what kind 9 is for, and what its leading Identity names. Treat a silent
result from any version of this as "none found", never "none exists".

Two more things before reading the output. It does not separate an emit from
a connect - both reach GetInstance and add an offset - so a row in Gamecode is
usually the sender and one in GUI.dll usually the subscriber, and which is
which is worth confirming in the disassembly. And the handler is guessed as
the last address-sized immediate pushed before the call, which on an emit site
picks up the constant MSVC loads for __EH_prolog: PlaySound's emit of + 0x190
reports "handler 0x1014413C", which is that constant and not a handler.

    python SignalMap.py                       # every module that has any
    python SignalMap.py --module GUI.dll
    python SignalMap.py --offset 0x22c

Read-only; it disassembles the shipped images and changes nothing.
"""

from __future__ import annotations

import argparse
import os
import struct

import capstone
import pefile

import MessageIds

GET_INSTANCE = '?GetInstance@GlobalSignals_c@@SAPAV1@XZ'

# How far past the call the offset may sit. Three covers the common
# `lea ecx, [eax + off]` straight after; a few sites spill the pointer
# first and need more.
WINDOW = 3


def slots(pe):
    """IAT addresses of GlobalSignals_c::GetInstance, plus its jmp thunks."""
    pe.parse_data_directories(
        directories=[pefile.DIRECTORY_ENTRY['IMAGE_DIRECTORY_ENTRY_IMPORT']])
    found = set()
    for library in getattr(pe, 'DIRECTORY_ENTRY_IMPORT', []):
        for imported in library.imports:
            if imported.name and imported.name.decode('ascii', 'replace') == GET_INSTANCE:
                found.add(imported.address)
    return found


def scan(path, want_offset=None):
    pe = pefile.PE(path, fast_load=True)
    targets = slots(pe)
    if not targets:
        return []
    image_base = pe.OPTIONAL_HEADER.ImageBase
    text = next(s for s in pe.sections if s.Name.rstrip(b'\0') == b'.text')
    blob = text.get_data()
    text_va = image_base + text.VirtualAddress

    # MSVC reaches an import either directly or through a one-line thunk.
    thunks = set()
    for target in targets:
        pattern = b'\xff\x25' + struct.pack('<I', target)
        offset = blob.find(pattern)
        while offset >= 0:
            thunks.add(text_va + offset)
            offset = blob.find(pattern, offset + 1)

    disassembler = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    disassembler.detail = True
    disassembler.skipdata = True
    instructions = list(disassembler.disasm(blob, text_va))
    index = {ins.address: n for n, ins in enumerate(instructions)}

    # A third way to reach the import, and the one that was missing until
    # 2026-09-12: load it into a register once and call the register for each
    # signal. Any window connecting several handlers does that, so the misses
    # were concentrated exactly where the interesting sites are - GUI.dll's
    # mail window connects the inbox list handler at 0x10109EF6 through ebx,
    # and this reported the signal as having no subscriber at all.
    holds_import = set()
    import_reg_at = {}
    for number, instruction in enumerate(instructions):
        if instruction.id == 0:
            # a skipdata filler: it has no operands and asking for them raises
            import_reg_at[number] = frozenset(holds_import)
            continue
        if instruction.operands:
            first = instruction.operands[0]
            if first.type == capstone.x86.X86_OP_REG and instruction.mnemonic != 'call':
                holds_import.discard(first.reg)
        if (instruction.mnemonic == 'mov'
                and len(instruction.operands) == 2
                and instruction.operands[0].type == capstone.x86.X86_OP_REG
                and instruction.operands[1].type == capstone.x86.X86_OP_MEM
                and instruction.operands[1].mem.base == 0
                and (instruction.operands[1].mem.disp & 0xFFFFFFFF) in targets):
            holds_import.add(instruction.operands[0].reg)
        import_reg_at[number] = frozenset(holds_import)

    results = []
    for number, instruction in enumerate(instructions):
        if instruction.mnemonic != 'call':
            continue
        hit = False
        for operand in instruction.operands:
            if (operand.type == capstone.x86.X86_OP_MEM
                    and operand.mem.base == 0
                    and (operand.mem.disp & 0xFFFFFFFF) in targets):
                hit = True
            if (operand.type == capstone.x86.X86_OP_IMM
                    and (operand.imm & 0xFFFFFFFF) in thunks):
                hit = True
            if (operand.type == capstone.x86.X86_OP_REG
                    and operand.reg in import_reg_at.get(number, ())):
                hit = True
        if not hit:
            continue
        # The offset is applied to the returned pointer within a couple of
        # instructions; the handler is the last address-sized immediate
        # pushed just before the call.
        offset = None
        for following in instructions[number + 1:number + 1 + WINDOW]:
            for operand in following.operands:
                if (operand.type == capstone.x86.X86_OP_MEM
                        and operand.mem.base != 0
                        and operand.mem.disp):
                    offset = operand.mem.disp
                    break
                if (following.mnemonic == 'add'
                        and operand.type == capstone.x86.X86_OP_IMM):
                    offset = operand.imm
                    break
            if offset is not None:
                break
        handler = None
        for previous in reversed(instructions[max(0, number - 8):number]):
            if previous.mnemonic in ('mov', 'push'):
                for operand in previous.operands:
                    if operand.type == capstone.x86.X86_OP_IMM:
                        value = operand.imm & 0xFFFFFFFF
                        if value in index:
                            handler = value
                            break
            if handler is not None:
                break
        if want_offset is not None and offset != want_offset:
            continue
        results.append((instruction.address, offset, handler))
    return results


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument('--module')
    parser.add_argument('--offset', type=lambda v: int(v, 0))
    parser.add_argument('--window', type=int, default=WINDOW)
    args = parser.parse_args()
    globals()['WINDOW'] = args.window

    names = [args.module] if args.module else sorted(
        name for name in os.listdir(MessageIds.CLIENT)
        if name.lower().endswith(('.dll', '.exe')))
    for name in names:
        path = os.path.join(MessageIds.CLIENT, name)
        try:
            found = scan(path, args.offset)
        except Exception as error:                      # a few images are not PE
            continue
        if not found:
            continue
        print(name)
        for site, offset, handler in found:
            print('  0x%08X  signal +0x%-4X  handler %s'
                  % (site,
                     offset if offset is not None else 0,
                     ('0x%08X' % handler) if handler else '-'))


if __name__ == '__main__':
    main()
