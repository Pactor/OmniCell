r"""ClientText - the names the client keeps for its own enumerations.

    python ClientText.py                  list the categories
    python ClientText.py 2015             print one category
    python ClientText.py 2015 --small     only the ids that look like enum values

The client ships a text database, cd_image/text/text.mdb, and LDBface::GetText
reads it as GetText(category, id). Most of it is sentences, but a good part of
it is enumerations: category 2000 maps a stat id to its internal name, 2003 to
its display name, 2008 names the criterion operators, 2015 names the targets an
effect can have, 505 the body locations, 2010 the organization ranks. Reading
those is how a field stops being a number.

The format is an MMDB: the magic, a count, then that many category and offset
pairs, and then each category's block. A block is a bare run of id and offset
pairs that runs until the next block starts - there is no count in front of it,
and reading the first id as one is what made category 506 look like it held a
single entry indexed by a string hash. It holds 176.

ctext.ldb beside it is a different format, MLDB, a flat run of category, id,
length and text with no directory at all. The two do not hold the same
categories and the client reads both.
"""
from __future__ import annotations

import argparse
import os
import struct

import MessageIds as mi

MDB = os.path.join('cd_image', 'text', 'text.mdb')
LDB = os.path.join('cd_image', 'text', 'ctext.ldb')

# An id this small is an enumeration value rather than a string hash.
ENUM_LIMIT = 1000


def read_mdb(path=None):
    """category -> {id: text}, from the MMDB directory."""
    path = path or os.path.join(mi.CLIENT, MDB)
    data = open(path, 'rb').read()
    if data[:4] != b'MMDB':
        raise SystemExit(path + ' is not an MMDB')

    count = struct.unpack('<I', data[4:8])[0]
    directory = []
    at = 8
    for _ in range(count):
        category, offset = struct.unpack('<II', data[at:at + 8])
        at += 8
        directory.append((category, offset))

    # A block runs until the next one begins, which is the only thing that says
    # how long it is.
    ordered = sorted(directory, key=lambda row: row[1])
    bounds = {}
    for i, (category, offset) in enumerate(ordered):
        end = ordered[i + 1][1] if i + 1 < len(ordered) else len(data)
        bounds[category] = (offset, end)

    out = {}
    for category, (start, end) in bounds.items():
        entries = {}
        at = start
        while at + 8 <= end:
            identifier, offset = struct.unpack('<II', data[at:at + 8])
            at += 8
            if offset >= len(data):
                break
            entries[identifier] = data[offset:data.index(b'\x00', offset)].decode('latin-1')
        out[category] = entries
    return out


def read_ldb(path=None):
    """category -> {id: text}, from the flat MLDB."""
    path = path or os.path.join(mi.CLIENT, LDB)
    data = open(path, 'rb').read()
    if data[:4] != b'MLDB':
        raise SystemExit(path + ' is not an MLDB')

    out = {}
    at = 4
    while at + 12 <= len(data):
        category, identifier, length = struct.unpack('<III', data[at:at + 12])
        at += 12
        if length > len(data) or at + length > len(data):
            break
        out.setdefault(category, {})[identifier] = (
            data[at:at + length].rstrip(b'\x00').decode('latin-1'))
        at += length
    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('category', nargs='?', type=int)
    parser.add_argument('--small', action='store_true',
                        help='only ids below %d, which are enumeration values' % ENUM_LIMIT)
    parser.add_argument('--ldb', action='store_true', help='read ctext.ldb instead of text.mdb')
    args = parser.parse_args()

    table = read_ldb() if args.ldb else read_mdb()

    if args.category is None:
        for category in sorted(table):
            entries = table[category]
            small = sum(1 for k in entries if k < ENUM_LIMIT)
            first = sorted(entries)[:2]
            print('%-8d %5d entries, %4d of them enumeration ids   %s' % (
                category, len(entries), small,
                '; '.join('%d=%s' % (k, entries[k][:30]) for k in first)))
        return

    entries = table.get(args.category)
    if not entries:
        raise SystemExit('no category %d' % args.category)
    for identifier in sorted(entries):
        if args.small and identifier >= ENUM_LIMIT:
            continue
        print('%12d  %s' % (identifier, entries[identifier]))


if __name__ == '__main__':
    main()
