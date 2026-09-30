r"""The client's own sentences, looked up the way the client looks them up.

    python LdbText.py TimeUntilGasChanges        one key
    python LdbText.py --sites                    every GetText call in Gamecode
    python LdbText.py --grep "skill is locked"   every line containing text
    python LdbText.py --id 1000                  every line in one category
    python LdbText.py --dump                     all of them

Gamecode does not hold the words it prints. It holds a category number and a
key - `push 0x3E8; push "TimeUntilGasChanges"` - and the sentence lives in
cd_image/text/ctext.ldb, filed under the ELF hash of that key. So a field whose
meaning is unclear from the disassembly is often written out in English one
lookup away:

    TimeUntilGasChanges
    \r\n<font color=CCInfoHeader>Time until supppression field changes to
    %u%%:</font> %02d:%02d:%02d

which names both of InfoPacket's suppression fields and their units, and gets
the developers' spelling of suppression thrown in.

There are two files and this reads both. `ctext.ldb` is from 2012 and is a
flat run of records - category, hash, length, text. `text.mdb` is from 2020,
is the one the client actually ships against, and is a directory of categories
each pointing at a sorted table of hash and offset pairs. They are not the same
set, and the difference matters: OrgInfo and OrgInfoRank, the two templates
that name seven of OrgServer's eight kind 2 strings, are only in the newer
file, and looking either up in the old one alone reports it missing. The hash is the ElfHash that ldb.dll exports, and it
is what the key turns into; the key strings themselves are in the code, not in
the database, so a lookup by name only works for a key you have already found in
a disassembly. `--grep` is the way round that.

`--sites` is the other way round again, and it is what found the OrgServer
templates. It lists every place a module calls LDBface::GetText together with
the key pushed into it, so a packet's dispatcher can be checked for one
directly: an address inside the branch you are reading means that branch
builds a sentence, and the sentence names its own fields. Running it over
Gamecode finds 134, and one of them at 0x101273AB inside OrgServer's kind 2
turned out to be LC_AreaInfo - "In: %s / Area: %s / Type: %s / Level: %d" -
which named two int32s that three packet pages had been calling reserved
metadata.

`--keys` goes the other way, and is the useful one for naming a field. The key
strings live in the code and the text lives in the file, so hashing every
printable string in every client image and matching the results against the
database recovers about 1150 key-and-sentence pairs - the client's own names for
the things it says. That is where SuppressionField75, DefenderRank and
NumInvadersKilled come from.

Two things it has to be careful about. Some categories - 100 and 110 among them -
are keyed by a small sequential id rather than a hash, and a small number
collides with any number of strings, so those are skipped. And a run of one
repeated character hashes like almost anything, so where several strings match a
hash the one that looks like an identifier wins.

Only the sentences the game code prints are here. GUI labels are their own
resource, and a key that is missing from this file is likely in one.
"""
from __future__ import annotations

import argparse
import glob
import itertools
import os
import re
import struct

import MessageIds as mi

TEXT = os.path.join('E:', os.sep, 'Funcom', 'Anarchy Online', 'cd_image', 'text')
LDB = os.path.join(TEXT, 'ctext.ldb')
MDB = os.path.join(TEXT, 'text.mdb')


def elf_hash(name):
    """ldb.dll's ElfHash, which is what a key is filed under."""
    value = 0
    for char in name.encode('latin1'):
        value = ((value << 4) + char) & 0xFFFFFFFF
        high = value & 0xF0000000
        if high:
            value ^= high >> 24
        value &= ~high & 0xFFFFFFFF
    return value


def ldb_entries(path=LDB):
    """ctext.ldb: a flat run of category, hash, length, text after a header."""
    data = open(path, 'rb').read()
    at = 4
    while at + 12 <= len(data):
        category, key, length = struct.unpack_from('<III', data, at)
        at += 12
        if length > len(data) - at or length > 0x10000:
            at -= 11
            continue
        yield category, key, data[at:at + length].decode('latin1')
        at += length


def mdb_entries(path=MDB):
    """text.mdb: the file ctext.ldb turned into, and the one that is current.

    Header is "MMDB" and a version, then a directory of category and offset
    pairs running up to the first category's offset, the last of them a
    0xFFFFFFFF terminator whose offset is where the tables stop. Each category
    is a sorted run of hash and offset pairs, and the text at that offset is
    NUL terminated.
    """
    if not os.path.exists(path):
        return
    data = open(path, 'rb').read()
    if data[:4] != b'MMDB':
        return
    first = struct.unpack_from('<I', data, 12)[0]
    directory = []
    at = 8
    while at + 8 <= first:
        category, offset = struct.unpack_from('<II', data, at)
        if offset == 0:
            break
        directory.append((category, offset))
        at += 8
    for index, (category, offset) in enumerate(directory):
        if category == 0xFFFFFFFF:
            continue
        end = directory[index + 1][1] if index + 1 < len(directory) else len(data)
        for pair in range(offset, end - 7, 8):
            key, where = struct.unpack_from('<II', data, pair)
            if where >= len(data):
                continue
            text = data[where:].split(b'\0')[0].decode('latin1')
            yield category, key, text


def entries(path=None):
    """Both files, newest first.

    ctext.ldb is from 2012 and text.mdb from 2020, and they are not the same
    set: OrgInfo, the template that names six of OrgServer's kind 2 strings,
    is only in the newer one. Reading the old file alone reports a key as
    missing when the client would find it.
    """
    if path is not None:
        return ldb_entries(path)
    return itertools.chain(mdb_entries(), ldb_entries())


GET_TEXT = {
    'Gamecode.dll': 0x101554B8,
}


def sites(module):
    """Every LDBface::GetText call in a module, with the key pushed into it.

    The import differs per module, so only the ones with a known thunk are
    listed; add to GET_TEXT when another module needs one.
    """
    import pefile

    thunk = GET_TEXT.get(module)
    if thunk is None:
        print('%s: no GetText thunk recorded - add it to GET_TEXT' % module)
        return
    path = os.path.join(mi.CLIENT, module)
    data = open(path, 'rb').read()
    pe = pefile.PE(path)
    base = pe.OPTIONAL_HEADER.ImageBase
    spans = [(base + s.VirtualAddress, s.Misc_VirtualSize, s.PointerToRawData)
             for s in pe.sections]
    cache = {}

    def text_at(va):
        if va in cache:
            return cache[va]
        found = None
        for lo, size, raw in spans:
            if lo <= va < lo + size:
                blob = data[raw + (va - lo):raw + (va - lo) + 80].split(b'\0')[0]
                if 3 <= len(blob) <= 48 and all(32 <= c < 127 for c in blob):
                    found = blob.decode()
        cache[va] = found
        return found

    # Two shapes reach the same import. A one-off is `call dword ptr [thunk]`.
    # A branch that builds several sentences loads it once - `mov ebx, [thunk]`
    # - and then `call ebx` each time, and the first cut of this missed every
    # one of those: OrgInfo and OrgInfoRank, the two that named seven of
    # OrgServer's kind 2 strings, are both called through ebx.
    found_at = []
    direct = bytes([0xFF, 0x15]) + struct.pack('<I', thunk)
    at = data.find(direct)
    while at >= 0:
        found_at.append(at)
        at = data.find(direct, at + 1)

    # mov r32, dword ptr [imm32] is 8B with modrm 00-reg-101; call r32 is FF D0+reg
    for reg in (0, 1, 2, 3, 6, 7):
        load = bytes([0x8B, 0x05 | (reg << 3)]) + struct.pack('<I', thunk)
        call_reg = bytes([0xFF, 0xD0 + reg])
        at = data.find(load)
        while at >= 0:
            window = data[at:at + 0x800]
            k = window.find(call_reg)
            while k >= 0:
                found_at.append(at + k)
                k = window.find(call_reg, k + 1)
            at = data.find(load, at + 1)

    for at in sorted(set(found_at)):
        key = None
        # the key is the last string pushed before the call; walk the pushes
        # back rather than a fixed window, because the category and the
        # destination buffer sit between them
        where = at
        for _ in range(10):
            where -= 1
            while where > 0 and data[where] != 0x68:
                where -= 1
            if where <= 0:
                break
            found = text_at(struct.unpack_from('<I', data, where + 1)[0])
            if found:
                key = found
                break
        print('0x%08X  %s' % (mi._to_va(path, at), key or '(no key found)'))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('key', nargs='?')
    parser.add_argument('--grep')
    parser.add_argument('--id', type=int)
    parser.add_argument('--dump', action='store_true')
    parser.add_argument('--sites', metavar='MODULE', nargs='?', const='Gamecode.dll',
                        help='every GetText call in a module, with its key')
    parser.add_argument('--keys', action='store_true',
                        help='recover key names by hashing every string in the client')
    args = parser.parse_args()

    if args.sites:
        sites(args.sites)
        return

    if args.keys:
        # several strings can hash alike, and a run of one repeated character
        # collides with almost anything - prefer the one that looks like a key
        def plausible(text):
            longest = 1
            run = 1
            for before, char in zip(text, text[1:]):
                run = run + 1 if char == before else 1
                longest = max(longest, run)
            return (longest <= 3, any(c.isupper() for c in text),
                    '_' in text or any(c.islower() for c in text), -len(text))

        names = {}
        for module in sorted(glob.glob(os.path.join(mi.CLIENT, '*.dll'))):
            data = open(module, 'rb').read()
            for match in re.finditer(rb'[A-Za-z_][A-Za-z0-9_]{3,48}', data):
                text = match.group(0).decode('latin1')
                key = elf_hash(text)
                if key not in names or plausible(text) > plausible(names[key]):
                    names[key] = text
        for category, key, text in entries():
            # some categories are keyed by a small sequential id rather than a
            # hash, and a small number collides with any number of strings
            if key < 0x10000:
                continue
            name = names.get(key)
            if name:
                print('%-6d %-34s %s' % (category, name, ' | '.join(text.splitlines())[:150]))
        return

    if args.key:
        wanted = elf_hash(args.key)
        found = False
        for category, key, text in entries():
            if key == wanted:
                found = True
                print('%-6d %08X  %s' % (category, key, text))
        if not found:
            print('%s hashes to %08X and is not in this file' % (args.key, wanted))
        return

    for category, key, text in entries():
        if args.grep and args.grep.lower() not in text.lower():
            continue
        if args.id is not None and category != args.id:
            continue
        if args.grep or args.id is not None or args.dump:
            print('%-6d %08X  %s' % (category, key, text.replace('\r\n', ' | ')[:200]))


if __name__ == '__main__':
    main()
