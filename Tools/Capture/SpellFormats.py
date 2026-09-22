r"""SpellFormats - the client's own description of every spell effect.

    python SpellFormats.py                 print the table
    python SpellFormats.py --csharp PATH   write the generated C# table

An effect on the wire is GameData::SpellData_t: an identity whose type half is
the game function, a version, a criteria list, and then a run of arguments
whose number and kinds depend on the function. Nothing in the message says how
long that run is, which is why this had been a hand-measured table of fourteen
functions - every function nobody had captured was unreadable, and one of the
fourteen was wrong in a way no capture could have shown.

The client does not measure. GameData.dll builds a SpellFormat_c for every
function it knows and puts them in a map, and it does it in code:
SpellFormats_c::SpellFormats_c at 0x1000FB0A is six thousand instructions of
new, Add, Add, Add, map-insert, in a straight line with no branches. That makes
it readable mechanically, which is what this does.

Each Add takes a ComplexType_e, a SpellStat_e and a default. Every argument is
four bytes on the wire except ComplexType 1, which is a length-prefixed string:
SpellFormat_c::BinaryToValue at 0x1000F01D has exactly those two arms, and
0x1000EAC0 is the one-line function that chooses between them - string for 1,
int for everything else.

Four arguments come before any of the per-function ones, because the
SpellFormat_c constructor at 0x1000FA93 adds them itself: SpellStat 3 with a
default of 1, SpellStat 4 with 0, SpellStat 32 with 0, and SpellStat 35 with -1.
Those are the four integers every effect carries between its criteria and its
arguments.
"""
from __future__ import annotations

import argparse
import io
import os
import re

import capstone
import pefile

import MessageIds as mi

MODULE = 'GameData.dll'
CTOR = 0x1000FB0A

NEW_FORMAT = 0x1000FA93      # SpellFormat_c::SpellFormat_c(bool, bool, int)
ADD_STAT = 0x1000FA05        # Add(ComplexType_e, SpellStat_e, int)
ADD_STRING = 0x1000FA4C      # Add(ComplexType_e, unsigned, string const&)
ADD_ARGUMENT = 0x1000F9ED    # Add(SpellArgument_c const&)
MAP_AT = 0x10013F90          # map<TypeID_e, SpellFormat_c*>::operator[]

# Everything else the constructor calls, and how many bytes of arguments each
# takes off the stack on the way out. operator new is cleaned up at the call
# site, so it is nothing.
OTHER_CALLS = {
    0x1001B23C: 0,   # operator new
    0x1001B254: 0,   # SEH prologue
    0x10001462: 8,
    0x100017AD: 8,
    0x1000F389: 8,   # SpellFormats_c::Add(TypeID_e, SpellFormat_c*)
    0x10001934: 4,
    0x1000720D: 8,
    0x10014484: 0,
}

STRING_TYPE = 1

COMMON = ((0, 3, 1), (0, 4, 0), (4, 32, 0), (0, 35, -1))

# Capstone prints a small immediate in decimal and a large one in hex, and
# a negative one as its unsigned hex. All three turn up in this constructor.
LITERAL = r'-?(?:0x[0-9a-f]+|\d+)'


def _value(text):
    value = int(text, 0)
    return value - 0x100000000 if value >= 0x80000000 else value


def _instructions():
    path = os.path.join(mi.CLIENT, MODULE)
    data = open(path, 'rb').read()
    pe = pefile.PE(data=data, fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    section = next(s for s in pe.sections if s.Name.startswith(b'.text'))
    start = base + section.VirtualAddress
    code = data[section.PointerToRawData:section.PointerToRawData + section.SizeOfRawData]
    offset = CTOR - start
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    return md.disasm(code[offset:], CTOR)


def read_table():
    """game function -> {'version': int, 'arguments': [(complex, stat, default)]}"""
    stack = []
    registers = {}
    slots = {}
    formats = {}
    mapping = {}
    current = None
    lea_slot = None

    def pop():
        return stack.pop() if stack else None

    for ins in _instructions():
        mnemonic, operands = ins.mnemonic, ins.op_str

        if mnemonic == 'mov':
            m = re.match(r'dword ptr \[ebp - (0x[0-9a-f]+)\], (%s)$' % LITERAL, operands)
            if m:
                slots[m.group(1)] = _value(m.group(2))
                continue
            m = re.match(r'(e\w\w), (%s)$' % LITERAL, operands)
            if m:
                registers[m.group(1)] = _value(m.group(2))
                continue
            m = re.match(r'(e\w\w), (e\w\w)$', operands)
            if m:
                registers[m.group(1)] = registers.get(m.group(2))
                continue
            continue

        if mnemonic == 'lea':
            m = re.match(r'e\w\w, \[ebp - (0x[0-9a-f]+)\]$', operands)
            if m:
                lea_slot = m.group(1)
            continue

        if mnemonic == 'xor' and re.match(r'(e\w\w), \1$', operands):
            registers[operands.split(',')[0]] = 0
            continue

        if mnemonic == 'pop':
            registers[operands] = pop()
            continue

        if mnemonic == 'push':
            if re.match(r'^e\w\w$', operands):
                stack.append(registers.get(operands))
            elif re.match(r'^%s$' % LITERAL, operands):
                stack.append(_value(operands))
            else:
                stack.append(None)
            continue

        if mnemonic == 'call':
            try:
                target = int(operands, 16)
            except ValueError:
                target = None
            if target == NEW_FORMAT:
                arguments = [pop() for _ in range(3)]
                current = len(formats) + 1
                formats[current] = {'version': arguments[2], 'arguments': list(COMMON)}
            elif target in (ADD_STAT, ADD_STRING):
                arguments = [pop() for _ in range(3)]
                if current is not None:
                    formats[current]['arguments'].append(tuple(arguments))
            elif target == ADD_ARGUMENT:
                pop()
            elif target == MAP_AT:
                pop()
                function = slots.get(lea_slot)
                if function is not None and current is not None:
                    mapping.setdefault(function, current)
            else:
                for _ in range(OTHER_CALLS.get(target, 0) // 4):
                    pop()
            registers.pop('eax', None)
            continue

        if mnemonic == 'ret':
            break

    return {function: formats[key] for function, key in mapping.items()}


def encode(argument):
    """One argument as the single int the generated table stores."""
    complex_type, stat, _ = argument
    return ((complex_type & 0xFFFF) << 16) | (stat & 0xFFFF)


HEADER = """// ----------------------------------------------------------------------------
// <copyright file="NanoEffectFormats.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Generated by Tools/Capture/SpellFormats.py. Do not edit by hand.
// </summary>
// ----------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System.Collections.Generic;

    /// <summary>
    /// What arguments each game function's effect carries.
    /// </summary>
    /// <remarks>
    /// Read out of the client rather than measured. GameData.dll builds a
    /// SpellFormat_c for every function it knows, in code, in one long
    /// branchless constructor at 0x1000FB0A, and Tools/Capture/SpellFormats.py
    /// walks it and writes this file.
    ///
    /// Each entry is one argument, packed as the ComplexType_e in the high
    /// sixteen bits and the SpellStat_e it binds to in the low sixteen. Every
    /// argument is four bytes on the wire except ComplexType 1, which is a
    /// length-prefixed string.
    ///
    /// The four arguments every effect carries before these - SpellStat 3, 4,
    /// 32 and 35 - are not in here. NanoEffect reads those as fields of its own,
    /// because every format has them and the SpellFormat_c constructor is where
    /// they come from rather than any one function's table entry.
    /// </remarks>
    public static class NanoEffectFormats
    {
        #region Constants

        /// <summary>
        /// The one ComplexType_e that is a string rather than four bytes.
        /// </summary>
        public const int StringArgument = 1;

"""

FOOTER = """
        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The arguments a function's effect carries, or null when the client
        /// carries no format for that function.
        /// </summary>
        public static int[] ArgumentsFor(int function)
        {
            int[] arguments;
            return Formats.TryGetValue(function, out arguments) ? arguments : null;
        }

        /// <summary>
        /// The ComplexType_e half of a packed argument.
        /// </summary>
        public static int KindOf(int argument)
        {
            return (argument >> 16) & 0xFFFF;
        }

        /// <summary>
        /// The SpellStat_e half of a packed argument.
        /// </summary>
        public static int StatOf(int argument)
        {
            return argument & 0xFFFF;
        }

        #endregion
    }
}
"""


def csharp(table, path):
    out = io.StringIO()
    out.write(HEADER)
    out.write('        private static readonly Dictionary<int, int[]> Formats =\n')
    out.write('            new Dictionary<int, int[]>\n                {\n')
    rows = []
    for function in sorted(table):
        extra = table[function]['arguments'][4:]
        values = ', '.join('0x%08X' % encode(a) for a in extra)
        if values:
            rows.append('                    { %d, new[] { %s } }' % (function, values))
        else:
            rows.append('                    { %d, new int[0] }' % function)
    out.write(',\n'.join(rows))
    out.write('\n                };\n')
    out.write(FOOTER)
    open(path, 'w', newline='\r\n').write(out.getvalue())
    return len(table)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--csharp')
    args = parser.parse_args()

    table = read_table()
    if args.csharp:
        print('%d game functions written to %s' % (csharp(table, args.csharp), args.csharp))
        return

    unresolved = sum(1 for f in table.values() for a in f['arguments'] if None in a)
    print('%d game functions, %d arguments with an unresolved default' % (len(table), unresolved))
    for function in sorted(table):
        extra = table[function]['arguments'][4:]
        fixed = sum(0 if a[0] == STRING_TYPE else 4 for a in extra)
        text = any(a[0] == STRING_TYPE for a in extra)
        print('%5d 0x%04X version %s  %3d bytes%s  %s' % (
            function, function, table[function]['version'], fixed,
            ' plus a string' if text else '              ',
            ' '.join('(%s,%s)' % (a[0], a[1]) for a in extra)))


if __name__ == '__main__':
    main()
