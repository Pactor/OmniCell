r"""Decode QuestFullUpdate the way the client does, and check it lands on the byte.

    python QuestConsume.py                    every capture in the capture directory
    python QuestConsume.py --verbose          print each record's fields

A round trip cannot catch a field boundary in the wrong place: a reader that
takes a string one byte short and a writer that puts it back one byte short
agree with each other perfectly. What catches it is decoding with an
*independent* description of the format and checking that the last field ends
exactly where the message does.

That description is the client's own reader, traced instruction by instruction
and written down in QuestInfo's remarks - Gamecode 0x100ABEA7 for the quest,
0x100870A8 for the reward box, 0x100ACBA0 for one action. This is that trace as
code. It knows nothing about the C# model, which is the point: when both agree
on a message, the shape is right twice over.

What a failure means. If a record stops short, the trace here is wrong or the
message uses a version this has not seen - the version is printed either way.
If it overruns, a length is being read as something else. Either is a finding;
neither is a reason to change this file until the disassembly says so.
"""
from __future__ import annotations

import argparse
import glob
import os
import struct
import zlib

CAPTURES = os.path.join('E:', os.sep, 'Funcom', 'captures')
OPCODE = bytes.fromhex('465a4061')


class Reader(object):
    def __init__(self, data, at=0):
        self.data = data
        self.at = at

    def int32(self):
        value = struct.unpack_from('>i', self.data, self.at)[0]
        self.at += 4
        return value

    def byte(self):
        value = self.data[self.at]
        self.at += 1
        return value

    def float(self):
        value = struct.unpack_from('>f', self.data, self.at)[0]
        self.at += 4
        return value

    def identity(self):
        return (self.int32(), self.int32())

    def raw(self, count):
        value = self.data[self.at:self.at + count]
        self.at += count
        return value

    def cstring(self):
        end = self.data.index(b'\0', self.at)
        value = self.data[self.at:end]
        self.at = end + 1
        return value

    def x3f1(self):
        """The count sentinel: (entries + 1) * 0x3F1."""
        value = self.int32()
        if value % 0x3F1:
            raise ValueError('count 0x%X is not an X3F1 sentinel' % value)
        return value // 0x3F1 - 1


def world_pos(r):
    r.identity()
    r.int32()
    r.int32()
    r.float(), r.float(), r.float()


def acg_item(r):
    return [r.int32() for _ in range(4)]


def reward_box(r):
    """0x100870A8."""
    version = r.int32()
    cash = r.int32()
    experience = r.int32()
    r.int32()
    for _ in range(r.x3f1()):
        r.identity()
    for _ in range(r.x3f1()):
        r.identity()
    for _ in range(r.x3f1()):
        acg_item(r)
    if version >= 4:
        r.int32(), r.int32(), r.int32()
    if version >= 5:
        r.int32(), r.int32()
    if version >= 6:
        acg_item(r)
    return version, cash, experience


def action(r):
    """0x100ACBA0."""
    r.int32()
    for _ in range(5):
        r.identity()
    for _ in range(4):
        r.float()
    r.identity()
    for _ in range(4):
        r.float()
    r.identity()
    r.int32()
    r.int32()
    r.identity()
    world_pos(r)


def quest(r):
    """0x100ABEA7."""
    version = r.int32()
    if not 7 <= version <= 15:
        raise ValueError('version %d outside 7 to 15' % version)
    r.int32()
    r.int32()
    flags = r.int32()
    name = r.cstring()
    length = r.int32()
    if length > 0xFFF:
        raise ValueError('description length 0x%X above 0xFFF' % length)
    description = r.raw(length)
    r.identity()
    box = reward_box(r)
    r.identity()
    icon = r.int32()
    r.int32()
    r.int32()
    for _ in range(r.x3f1()):
        action(r)
    for _ in range(r.x3f1()):
        r.identity()
    for _ in range(r.int32()):
        r.int32()
    for _ in range(r.int32()):
        r.int32()
    for _ in range(r.int32()):
        r.identity()
        r.raw(r.int32())
    if version >= 8:
        r.int32()
    if version >= 9:
        for _ in range(r.x3f1()):
            r.identity()
    if version >= 10:
        r.int32()
    if version >= 11:
        r.int32()
    if version >= 12:
        r.identity()
        r.int32()
        r.int32()
    if version >= 13:
        for _ in range(r.int32()):
            r.identity()
            r.int32()
    if version >= 14:
        r.int32()
    if version >= 15:
        # 0x100ABDE6, and its count is an X3F1 sentinel where the version 13
        # block's is a plain int32 - the two conventions sit four fields apart
        for _ in range(r.x3f1()):
            r.int32()
            r.int32()
    return version, name, flags, icon, box, description


def messages():
    for path in sorted(glob.glob(os.path.join(CAPTURES, '*_s*.csv'))):
        data = bytearray()
        for line in open(path):
            parts = line.strip().split(',')
            if len(parts) >= 3 and parts[1] == 'server':
                try:
                    data.extend(bytes.fromhex(parts[2]))
                except ValueError:
                    pass
        if len(data) < 20:
            continue
        try:
            body = zlib.decompressobj().decompress(bytes(data[16:]))
        except zlib.error:
            continue
        at = 0
        while at + 16 <= len(body):
            length = struct.unpack_from('>H', body, at + 6)[0]
            if length < 16 or at + length > len(body):
                break
            one, at = body[at:at + length], at + length
            if len(one) > 20 and one[16:20] == OPCODE:
                yield os.path.basename(path), one


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--verbose', action='store_true')
    args = parser.parse_args()

    total = exact = 0
    for name, message in messages():
        total += 1
        r = Reader(message, 29)
        try:
            count = r.x3f1()
            quests = []
            for _ in range(count):
                r.identity()
                quests.append(quest(r))
            r.byte()
        except Exception as error:                    # noqa: BLE001 - report, don't raise
            print('%-28s %4d bytes  FAILED at %d: %s' % (name, len(message), r.at, error))
            continue
        if r.at == len(message):
            exact += 1
            if args.verbose:
                for version, qname, flags, icon, box, description in quests:
                    print('   v%-3d %-34s flags %-6d icon %-8d box v%d cash %d xp %d  %d chars'
                          % (version, qname.decode('latin1')[:34], flags, icon,
                             box[0], box[1], box[2], len(description)))
        else:
            print('%-28s %4d bytes  ended at %d, %+d' % (name, len(message), r.at,
                                                         r.at - len(message)))
    print('')
    print('%d of %d consumed to the byte' % (exact, total))


if __name__ == '__main__':
    main()
