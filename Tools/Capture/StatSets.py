r"""StatSets - which stats a dispatcher writes, and from which message member.

    python StatSets.py Gamecode.dll 0x1007835C
    python StatSets.py Gamecode.dll 0x10063E88 400

The most productive single shape in this client. A dispatcher that puts a field
into a character stat does it like this:

    push  <the value>
    push  <the stat id>
    call  dword ptr [eax + 0x40]      SetStat on the table at [dynel + 0xE8]

and the constant in the second push is a StatIds entry. That is how mechdata
named MechInfo's last field, how personalresearchgoal named PerkUpdate's
second, and how shadowbreed named the byte behind SimpleCharFullUpdate's
0x20000000. When the value comes straight from a message member the push shows
that too - "pushes=['dword ptr [ebx + 0x31c]', '0x296']" is the whole answer to
what lives at + 0x31C.

Slots 0x44 and 0x48 on the same table are the add and the has-it check, and
0x3C is the getter, so all four are printed; a getter says the dispatcher is
reading a stat rather than writing one, which is worth telling apart.

Cross-check every id against OmniCell.Enums StatIds before believing it. A small
constant pushed before a virtual call is not always a stat id - the same shape
appears with array indexes and with flags - and the check is free:

    grep -n "= 532," OmniCell/Libraries/Source/OmniCell.Enums/StatIds.cs

Not every stat write goes to that table. SimpleCharFullUpdate's monster scale
goes to a map on the skill subsystem at [character + 0x1BC] instead, keyed by
the same stat id, and this tool will not show it - it only knows the one shape.
"""
from __future__ import annotations

import argparse
import os

import capstone

import MessageIds as mi

SETTERS = ('+ 0x40]', '+ 0x44]', '+ 0x48]', '+ 0x3c]')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('module')
    parser.add_argument('address', type=lambda v: int(v, 0))
    parser.add_argument('count', nargs='?', type=int, default=600)
    args = parser.parse_args()

    path = os.path.join(mi.CLIENT, args.module)
    data = open(path, 'rb').read()
    offset = mi._to_offset(path, args.address)
    if offset is None:
        raise SystemExit('address not in ' + args.module)

    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    window = []
    for ins in md.disasm(data[offset:offset + args.count * 8], args.address, args.count):
        window.append(ins)
        if len(window) > 6:
            window.pop(0)
        if ins.mnemonic != 'call' or not any(s in ins.op_str for s in SETTERS):
            continue
        pushes = [w.op_str for w in window[:-1] if w.mnemonic == 'push']
        print('0x%08X  %-22s  pushes=%s' % (ins.address, ins.op_str, pushes))
        for value in pushes:
            if not value.startswith('0x'):
                continue
            number = int(value, 16)
            if 1 <= number < 1200:
                print('%12s candidate stat id %d' % ('', number))


if __name__ == '__main__':
    main()
