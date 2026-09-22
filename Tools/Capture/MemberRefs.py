r"""MemberRefs - every instruction in the client that touches one object offset.

    python MemberRefs.py 0x1E4
    python MemberRefs.py 0x1E4 --module Gamecode.dll
    python MemberRefs.py 0x1D4 --all
    python MemberRefs.py 0x88 --context 6

The dispatcher of a message usually ends by copying its fields onto a live
object, and the field's name is then whatever else reads that member. Answering
that means scanning a whole text section for memory operands with one
displacement, which had been done by hand - badly - at least three times before
this existed: the write-ups for ServerPosDebugInfo, TrapItemFullUpdate and
LaserTargetList each say "as far as can be found" about an offset.

What it finds and what it does not:

  * the plain [register + 0xNNN] form, which is what a compiled member access
    looks like when the object pointer is already in a register
  * both reads and writes, in any instruction, not just mov

  * NOT an access computed off a pointer taken earlier into a local, which the
    compiler does when it touches several members of the same sub-object
  * NOT [register + register*scale + 0xNNN], where the displacement is an array
    base rather than a member

So an empty result is "no plain reference found", never "nothing reads this".
Say it that way in a write-up: the difference is what separates a measurement
from a guess.

Small displacements are everywhere - 0x88 has thousands of hits across the
client and almost none of them are the member being chased - so a result of
more than a few hundred is a sign the offset is too small to be identifying,
not a lead. Offsets above 0x100 are usually specific enough to be worth
reading.
"""
from __future__ import annotations

import argparse
import os
import re

import MessageIds as mi

MODULES = ('Gamecode.dll', 'N3.dll', 'Fanatic.dll', 'city.dll', 'Vehicle.dll',
           'Interfaces.dll', 'GUI.dll', 'AFCM.dll', 'DisplaySystem.dll')

# [reg + 0xNNN] and [reg - 0xNNN], but not [reg + reg*n + 0xNNN].
PLAIN = re.compile(r'\[e[a-z][a-z] ([-+]) (0x[0-9a-f]+)\]')


def scan(path, offset):
    """Every instruction in the image whose operand is [reg + offset].

    Anchored on the encoded displacement rather than disassembled straight
    through. A text section is not all code - jump tables, string literals and
    alignment padding sit in the middle of it - and a linear sweep from the
    section start desynchronises on the first of them and stays wrong for a
    long stretch afterwards. The first version of this did exactly that and
    reported two references to + 0x1E4 in the whole of Gamecode, both of them
    stack locals, while missing the one the caller was looking at.

    So: find the four displacement bytes, then try to disassemble an
    instruction ending on them from each of the few preceding byte positions an
    x86 instruction could start at. An instruction that decodes cleanly, ends
    exactly where the displacement ends, and names the offset in its operands
    is a real reference.
    """
    import capstone
    import pefile
    import struct

    data = open(path, 'rb').read()
    pe = pefile.PE(data=data, fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)

    wanted = '0x%x]' % offset
    needles = [struct.pack('<I', offset)]
    if offset < 0x80:
        needles.append(bytes([offset]))

    hits = []
    seen = set()
    for section in pe.sections:
        if not section.Characteristics & 0x20000000:  # executable
            continue
        start = base + section.VirtualAddress
        raw = section.PointerToRawData
        code = data[raw:raw + section.SizeOfRawData]
        for needle in needles:
            at = code.find(needle)
            while at >= 0:
                end = at + len(needle)
                for back in range(2, 10):
                    if at - back < 0:
                        break
                    # The displacement is not always the last field: cmp byte
                    # ptr [esi + 0x1E5], 0 puts an immediate after it, and an
                    # earlier version of this required the instruction to end
                    # on the displacement and so reported that TrapItem_t's
                    # armed byte was written and never read. Allow up to four
                    # trailing immediate bytes, and let the operand text be
                    # what decides.
                    for tail in range(0, 5):
                        window = code[at - back:end + tail]
                        for instruction in md.disasm(window, start + at - back, 1):
                            if instruction.size != back + len(needle) + tail:
                                continue
                            if wanted not in instruction.op_str:
                                continue
                            if instruction.address in seen:
                                continue
                            seen.add(instruction.address)
                            hits.append((instruction.address, instruction.mnemonic,
                                         instruction.op_str))
                at = code.find(needle, at + 1)
    hits.sort()
    return hits


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('offset', type=lambda v: int(v, 0))
    parser.add_argument('--module', default='Gamecode.dll')
    parser.add_argument('--all', action='store_true', help='every shipped module')
    parser.add_argument('--context', type=int, default=0,
                        help='disassemble this many instructions at each hit')
    args = parser.parse_args()

    import Reader

    modules = MODULES if args.all else (args.module,)
    for module in modules:
        path = os.path.join(mi.CLIENT, module)
        if not os.path.exists(path):
            continue
        hits = scan(path, args.offset)
        if not hits:
            continue
        print('%s: %d references to + 0x%X' % (module, len(hits), args.offset))
        for address, mnemonic, operands in hits:
            print('  0x%08X  %-9s %s' % (address, mnemonic, operands))
            if args.context:
                for line in Reader.disassemble(path, address, args.context):
                    print('  ' + line)
                print()


if __name__ == '__main__':
    main()
