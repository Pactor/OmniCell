r"""ClassHierarchy - every class in the hierarchy of a given one, with vtables.

    python ClassHierarchy.py Beholder_t          just the classes
    python ClassHierarchy.py Beholder_t 12       and what each puts in slot 12

Why this exists: a message dispatcher hands the packet's fields to a virtual
call on some interface, and the interface is where the field names live. The
interface itself is abstract, so its own vtable slots say nothing. This finds
the classes that implement it.

It answered the question it was written for immediately. SpellList hands its
fields to slot 12 of a Beholder_t, and the fourteen classes below Beholder_t
are AccessCard, CentralController, CityTerminal, DummyItemBase, DummyWeapon,
LockableItem, NanoItem, QuestBooth, ReclaimBooth, SimpleItem, TrapItem,
WeaponItem and WearableItem - every one of them an item, which is what the
identity in that message must be.

The chain is the compiler's own, and none of it is guesswork:

    type descriptor  <- RTTIBaseClassDescriptor.pTypeDescriptor
    base class descriptor  <- an entry in a base class array
    base class array  <- RTTIClassHierarchyDescriptor.pBaseClassArray
    hierarchy descriptor  <- RTTICompleteObjectLocator.pClassDescriptor
    complete object locator  <- the dword before a vtable's first entry
"""
import importlib.util
import struct
import sys

spec = importlib.util.spec_from_file_location('mi', r'E:\Funcom\OmniCell\Tools\Capture\MessageIds.py')
mi = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mi)

PATH = r'E:\Funcom\Anarchy Online\Gamecode.dll'
DATA = open(PATH, 'rb').read()


def refs(va):
    packed = struct.pack('<I', va)
    at = 0
    while True:
        at = DATA.find(packed, at)
        if at < 0:
            return
        yield at
        at += 1


def descriptor_of(name):
    at = DATA.find(('.?AV' + name + '@@').encode('latin1') + b'\x00')
    return None if at < 0 else mi._to_va(PATH, at - 8)


def name_of_descriptor(va):
    off = mi._to_offset(PATH, va)
    if off is None:
        return None
    return DATA[off + 8:off + 160].split(b'\x00')[0].decode('latin1')


def derived(name):
    target = descriptor_of(name)
    if target is None:
        return []

    # base class descriptors naming it: the descriptor pointer is their first field
    bcds = [mi._to_va(PATH, off) for off in refs(target)]
    bcds = [v for v in bcds if v is not None]

    # base class arrays holding one of those
    arrays = set()
    for bcd in bcds:
        for off in refs(bcd):
            va = mi._to_va(PATH, off)
            if va is not None:
                arrays.add(va)

    found = {}
    for entry in arrays:
        # the array may be pointed at from its own start or from further in;
        # a hierarchy descriptor points at the start
        for base in (entry, entry - 4, entry - 8, entry - 12):
            for off in refs(base):
                # RTTIClassHierarchyDescriptor: signature, attributes, numBase, pArray
                chd_off = off - 12
                if chd_off < 0:
                    continue
                sig, attrs, num = struct.unpack_from('<3I', DATA, chd_off)
                if sig != 0 or num == 0 or num > 64:
                    continue
                chd = mi._to_va(PATH, chd_off)
                if chd is None:
                    continue
                for coff in refs(chd):
                    # RTTICompleteObjectLocator: sig, offset, cd, pTypeDesc, pChd
                    col_off = coff - 16
                    if col_off < 0 or struct.unpack_from('<I', DATA, col_off)[0] != 0:
                        continue
                    td = struct.unpack_from('<I', DATA, col_off + 12)[0]
                    cname = name_of_descriptor(td)
                    if not cname:
                        continue
                    col = mi._to_va(PATH, col_off)
                    for voff in refs(col):
                        vt_off = voff + 4
                        try:
                            slots = struct.unpack_from('<16I', DATA, vt_off)
                        except struct.error:
                            continue
                        if not all(s >= 0x10001000 and mi._to_offset(PATH, s) is not None
                                   for s in slots[:9]):
                            continue
                        found[cname] = (mi._to_va(PATH, vt_off), slots)
    return found


if __name__ == '__main__':
    name = sys.argv[1]
    slot = int(sys.argv[2]) if len(sys.argv) > 2 else None
    hits = derived(name)
    print('%d classes in the hierarchy of %s' % (len(hits), name))
    for cname in sorted(hits):
        vtable, slots = hits[cname]
        line = '   %-40s vtable 0x%08X' % (cname, vtable)
        if slot is not None:
            line += '   slot %d 0x%08X' % (slot, slots[slot])
        print(line)
