r"""MessageIds - work out an AO message id from its class name, and back.

    python MessageIds.py <name>...      hash one or more class names
    python MessageIds.py --audit        check every client class against our enum
    python MessageIds.py --rtti <name>  find a class's reader and writer

Every N3 message id is a hash of the client's own class name. The client
registers each message class by name, and the registrar turns the name into the
id with this, at N3.dll RVA 0x9826:

    id = 0
    for i, c in enumerate(name):
        id ^= signed_byte(c) << ((i & 3) * 8)

That is the whole of it: each character is XORed into one of the four bytes of
the id, chosen by its position modulo four. It is why searching the binaries for
an opcode finds nothing - the id is never stored anywhere, only computed.

The names themselves are in the client as plain strings, because the registrar
takes a std::string. Gamecode.dll holds a hundred of them, N3.dll four and
Fanatic.dll one, all ending in "IIR_t":

    strings -n 6 *.dll | grep "IIR_t$"

Ninety five of those hash to ids already in N3MessageType, which is what
confirms all of this rather than merely suggesting it. Three disagree and five
have no entry at all; see --audit and the handoff.

What this is for: the authoritative name of a message is the way into the
client's code for it. n3InfoItemRemote_t::Construct at RVA 0x9B08 looks the id
up in a registry and calls the factory it finds, so a name gives an id, an id
gives a registration, and a registration gives the reader that knows the packet
layout. That is the route for every packet still short of green.
"""
import glob
import os
import re
import struct
import subprocess
import sys

CLIENT = r'E:\Funcom\Anarchy Online'
ENUM = (r'E:\Funcom\OmniCell\OmniCell\Libraries\Source\AOtomation.Messaging'
        r'\SmokeLounge.AOtomation.Messaging\Messages\N3MessageType.cs')


def message_id(name):
    """The id the client computes for a class name."""
    value = 0
    for i, ch in enumerate(name.encode('latin1')):
        signed = ch if ch < 128 else ch - 256
        value ^= (signed << ((i & 3) * 8)) & 0xFFFFFFFF
    return value & 0xFFFFFFFF


# The client does not settle on one suffix. Most message classes end IIR_t, but
# FovIIR_c, AppearanceUpdateIIR_c, KnubotOpenChatWindowIIR_c, ResearchUpdateIIR
# and FightModeUpdate_t all exist, and a scan for IIR_t alone reported every one
# of them as a message the client does not carry. Since the id is a hash of the
# whole name, the suffix cannot be guessed - so hash every identifier in the
# binaries and let the id say which ones are message classes.
CLASS_NAME = re.compile(r'^[A-Za-z_][A-Za-z0-9_]{3,60}$')


def client_class_names():
    """Every identifier the client binaries carry, as candidate class names."""
    found = set()
    for path in glob.glob(os.path.join(CLIENT, '*.dll')) + glob.glob(os.path.join(CLIENT, '*.exe')):
        try:
            data = open(path, 'rb').read()
        except OSError:
            continue
        for match in re.finditer(rb'[A-Za-z_][A-Za-z0-9_]{3,60}', data):
            name = match.group(0).decode('ascii')
            if CLASS_NAME.match(name):
                found.add(name)
    return sorted(found)


def our_ids():
    text = open(ENUM, encoding='utf-8-sig', errors='replace').read()
    return {m.group(1): int(m.group(2), 16)
            for m in re.finditer(r'(\w+)\s*=\s*(0x[0-9A-Fa-f]{8})', text)}


def core(name):
    for suffix in ('IIR_t', 'IIR_c', 'IIR', '_t', '_c'):
        if name.endswith(suffix):
            name = name[:-len(suffix)]
            break
    return name[2:] if name.startswith('n3') else name


def audit():
    names = client_class_names()
    ours = our_ids()
    by_id = {v: k for k, v in ours.items()}
    lowered = {k.lower(): k for k in ours}

    agree = [n for n in names if message_id(n) in by_id]
    print('%d client class names, %d ids in N3MessageType, %d agree' % (len(names), len(ours), len(agree)))

    print('\ndisagree - same message, different id:')
    for n in names:
        c = core(n).lower()
        if c in lowered and ours[lowered[c]] != message_id(n):
            print('   %-34s client 0x%08X   ours 0x%08X (%s)'
                  % (n, message_id(n), ours[lowered[c]], lowered[c]))

    print('\nno entry in N3MessageType at all:')
    for n in names:
        if message_id(n) not in by_id and core(n).lower() not in lowered:
            print('   0x%08X  %s' % (message_id(n), n))


# ---------------------------------------------------------------- locating
# Where a class's reader and writer live, found the same way every time.
#
#   name string in Gamecode.dll
#     -> the instruction that pushes it (the registration)
#     -> the factory pushed beside it
#     -> the constructor the factory calls
#     -> the vtable the constructor installs
#     -> slot 7 is ReadSubClass, slot 8 is WriteSubClass
#
# The slot numbers are not a guess. n3PlayfieldFullUpdateIIR_t exports both by
# name, and in its vtable at N3.dll 0x3D0CC they sit at 7 and 8.

SECTIONS = {}


def _sections(path):
    """name -> (virtual address, file offset, size), from the PE headers."""
    if path in SECTIONS:
        return SECTIONS[path]
    out = subprocess.check_output(['objdump', '-h', path], stderr=subprocess.DEVNULL).decode('latin1')
    table = {}
    for line in out.splitlines():
        m = re.match(r'\s*\d+\s+(\.\w+)\s+([0-9a-f]+)\s+([0-9a-f]+)\s+[0-9a-f]+\s+([0-9a-f]+)', line)
        if m:
            table[m.group(1)] = (int(m.group(3), 16), int(m.group(4), 16), int(m.group(2), 16))
    SECTIONS[path] = table
    return table


def _to_va(path, file_offset):
    for va, off, size in _sections(path).values():
        if off <= file_offset < off + size:
            return va + (file_offset - off)
    return None


def _to_offset(path, va):
    for sva, off, size in _sections(path).values():
        if sva <= va < sva + size:
            return off + (va - sva)
    return None


def _installs_vtable(data, path, func_va):
    """The vtable a constructor writes into its object, if it is one."""
    off = _to_offset(path, func_va)
    if off is None:
        return None
    body = data[off:off + 0x100]
    # movl $imm,(reg). The modrm byte names the register: 00 eax, 01 ecx,
    # 02 edx, 03 ebx, 06 esi, 07 edi. 04 is a SIB byte and 05 a bare disp32,
    # and neither is a store through a register.
    #
    # This stopped at 03 until 2026-09-11, and that is what sent
    # n3TeleportIIR_t wrong. Its constructor keeps the object in esi and
    # writes mov dword ptr [esi], 0x1003E68C, which was invisible here - so
    # the walk gave up on the right factory, carried on, and came back with
    # the class registered beside it. Everything downstream of that was
    # coherent and about a different message.
    for m in re.finditer(b'\xc7([\x00-\x03\x06\x07])(....)', body):
        vtable = struct.unpack('<I', m.group(2))[0]
        vt_off = _to_offset(path, vtable)
        if vt_off is None:
            continue
        slots = struct.unpack_from('<9I', data, vt_off)
        # a real vtable here is nine pointers, all into code
        if all(s >= 0x10001000 and _to_offset(path, s) is not None for s in slots):
            return vtable, slots
    return None


def locate(name, path=None):
    """Find ReadSubClass and WriteSubClass for a message class.

    The route is the same every time, and none of it is guesswork:

        the name string, which the registrar takes as a std::string
        -> the instruction that pushes it
        -> the factory pushed beside it in the same registration
        -> the constructor that factory calls, which is not the first call it
           makes - that one is SEH setup
        -> the vtable that constructor installs
        -> slots 7 and 8

    The slot numbers come from n3PlayfieldFullUpdateIIR_t, which exports both
    ReadSubClass and WriteSubClass by name; in its vtable they sit at 7 and 8.

    Two things about the walk, both learned the hard way on n3TeleportIIR_t on
    2026-09-11.

    The registration is one shape and the scan now insists on it. The registrar
    takes a std::string, so the name is pushed first for the string
    constructor, and the factory is pushed with the finished string right
    before the call:

        push <name>            build the std::string
        ...
        push <factory>
        push <the string>
        call <register>

    which on the wire is 68 <imm32> 50 E8, or 51 / 52 for the other registers.
    The old scan looked at every push in a window from 0x80 before the name to
    0x140 after it and took the first one that led to a vtable. For
    n3TeleportIIR_t that found the factory of the registration *above* it, and
    handed back a whole coherent class - vtable 0x1003D0CC, a reader that takes
    a version and three floats - that had nothing to do with teleporting. It
    cost a note in the protocol docs saying the client "does not add up",
    because that reader disagreed with 32 of 33 captured copies.

    And the module matters. This used to default to Gamecode.dll, and the bases
    overlap: N3.dll has its own code at 0x10029C24 too. With no path it now
    tries every module and returns the first that holds the name.
    """
    if path is None:
        for candidate in sorted(glob.glob(os.path.join(CLIENT, '*.dll'))):
            found = locate(name, candidate)
            if found:
                return found
        return None

    # The compiler's own record first, when the class has one. Walking the
    # registration is a heuristic either way - it has to decide which of the
    # pointers near a name is the factory - and RTTI has nothing to decide.
    # QuestFullUpdateIIR_t is the standing proof: the sweep answers 0x10167C78
    # and the record answers 0x101683E8, and 0x101683E8 is the vtable holding
    # its known reader, writer and dispatcher.
    through_rtti = locate_rtti(name, path)
    if through_rtti:
        through_rtti['via'] = 'rtti'
        return through_rtti

    data = open(path, 'rb').read()

    at = data.find(name.encode('latin1') + b'\x00')
    if at < 0:
        return None
    string_va = _to_va(path, at)
    if string_va is None:
        return None

    def from_factory(factory):
        if factory == string_va or factory < 0x10001000:
            return None
        off = _to_offset(path, factory)
        if off is None:
            return None
        body = data[off:off + 0x60]
        for cm in re.finditer(b'\xe8(....)', body):
            rel = struct.unpack('<i', cm.group(1))[0]
            target = factory + cm.start() + 5 + rel
            found = _installs_vtable(data, path, target)
            if found:
                vtable, slots = found
                return {'string': string_va, 'factory': factory, 'constructor': target,
                        'vtable': vtable, 'ReadSubClass': slots[7], 'WriteSubClass': slots[8]}
        return None

    pushed = re.escape(b'\x68' + struct.pack('<I', string_va))
    sites = [m.start() for m in re.finditer(pushed, data)]

    # First pass: the exact shape of a registration that takes a std::string.
    # The name is pushed to build the string, and the factory goes in with the
    # finished string right before the call:
    #
    #     push <name>                    68 <imm32>
    #     ...                            build the std::string
    #     push <factory>                 68 <imm32>
    #     push <the string>              50 to 57
    #     call <register>                E8 <rel32>, or FF 15 for an import
    #
    # Both forms of the call matter. N3.dll calls its registrar directly and
    # Gamecode goes through the import table, and a rule that only knew E8
    # matched one module and silently fell through to the sweep for the other.
    # Only forward of the name, because the string has to exist first.
    for start in sites:
        window = data[start:start + 0x140]
        for pm in re.finditer(b'\x68(....)[\x50-\x57](?:\xe8|\xff\x15)', window):
            got = from_factory(struct.unpack('<I', pm.group(1))[0])
            if got:
                return got

    # Second pass: the old sweep of every push near the name. Most of the
    # client's registrations are not the shape above, so this is what answers
    # for them - but it is a guess at which pointer is the factory, and on
    # n3TeleportIIR_t it guessed the registration next door. Anything that
    # comes back from here is a candidate to check, not a finding.
    for start in sites:
        lo = max(0, start - 0x80)
        window = data[lo:start + 0x140]
        for pm in re.finditer(b'\x68(....)', window):
            got = from_factory(struct.unpack('<I', pm.group(1))[0])
            if got:
                return got
    return None


def locate_loose(name, path=None):
    """The old walk, kept because a registration that does not take a string
    will not match the strict one. Its answer is a candidate, not a finding:
    check the vtable against something the class must do."""
    path = path or os.path.join(CLIENT, 'Gamecode.dll')
    data = open(path, 'rb').read()

    at = data.find(name.encode('latin1') + b'\x00')
    if at < 0:
        return None
    string_va = _to_va(path, at)
    if string_va is None:
        return None

    pushed = re.escape(b'\x68' + struct.pack('<I', string_va))
    for m in re.finditer(pushed, data):
        lo = max(0, m.start() - 0x80)
        window = data[lo:m.start() + 0x140]
        for pm in re.finditer(b'\x68(....)', window):
            factory = struct.unpack('<I', pm.group(1))[0]
            if factory == string_va or factory < 0x10001000:
                continue
            off = _to_offset(path, factory)
            if off is None:
                continue
            body = data[off:off + 0x60]
            for cm in re.finditer(b'\xe8(....)', body):
                rel = struct.unpack('<i', cm.group(1))[0]
                target = factory + cm.start() + 5 + rel
                found = _installs_vtable(data, path, target)
                if found:
                    vtable, slots = found
                    return {'string': string_va, 'factory': factory, 'constructor': target,
                            'vtable': vtable, 'ReadSubClass': slots[7], 'WriteSubClass': slots[8]}
    return None


# ------------------------------------------------------------------ by RTTI
# A second route to the same place, and a better one.
#
# locate() above walks the registration, which means guessing which of the
# pointers pushed near a name is the factory. It is wrong more often than it is
# right - most names reach a base class vtable - and there is no way to tell
# from the result.
#
# This walks the compiler's own record instead, and there is nothing to guess:
#
#   ".?AVSpellListIIR_t@@"          the RTTI type descriptor's name
#     -> the type descriptor, which begins eight bytes before that string
#     -> the complete object locator, whose fourth dword points at it
#     -> the vtable, whose first entry follows a pointer to that locator
#     -> slot 7 is ReadSubClass, slot 8 is WriteSubClass
#
# One class has one type descriptor and one vtable, so a hit is the answer
# rather than a candidate. The only classes it cannot find are the ones the
# compiler emitted without RTTI.


def _rtti_names(name):
    """The decorated names the compiler writes for a class.

    A class inside a namespace decorates as ".?AVName@Namespace@@" rather
    than ".?AVName@@", so looking only for the bare form reports a class
    that plainly has RTTI as having none. ChatCmd is Fanatic::FanaticIIR_t
    and was written off that way for a while.
    """
    return [('.?AV' + name + '@@').encode('latin1'),
            ('.?AV' + name + '@').encode('latin1')]


def locate_rtti(name, path=None):
    """ReadSubClass and WriteSubClass, found through the RTTI records."""
    path = path or os.path.join(CLIENT, 'Gamecode.dll')
    data = open(path, 'rb').read()

    # The bare form ends at the name and is NUL-anchored; the namespaced one
    # carries the enclosing scopes after it, so it is not.
    at = -1
    for decorated in _rtti_names(name):
        at = data.find(decorated + b'\x00')
        if at < 0 and not decorated.endswith(b'@@'):
            at = data.find(decorated)
        if at >= 0:
            break
    if at < 0:
        return None

    # the type descriptor is [vftable][spare][name], so it starts eight
    # bytes before the name
    descriptor = _to_va(path, at - 8)
    if descriptor is None:
        return None

    packed = struct.pack('<I', descriptor)
    for m in re.finditer(re.escape(packed), data):
        # a complete object locator is signature, offset, cdOffset, then the
        # descriptor: so the locator begins twelve bytes earlier, and its
        # signature is zero
        head = m.start() - 12
        if head < 0 or struct.unpack_from('<I', data, head)[0] != 0:
            continue
        locator = _to_va(path, head)
        if locator is None:
            continue

        # the vtable carries a pointer to the locator immediately before its
        # first entry
        for vm in re.finditer(re.escape(struct.pack('<I', locator)), data):
            vtable = _to_va(path, vm.start() + 4)
            if vtable is None:
                continue
            vt_off = vm.start() + 4
            slots = struct.unpack_from('<9I', data, vt_off)
            if not all(s >= 0x10001000 and _to_offset(path, s) is not None for s in slots):
                continue
            return {'descriptor': descriptor, 'locator': locator, 'vtable': vtable,
                    'ReadSubClass': slots[7], 'WriteSubClass': slots[8]}
    return None


def is_stub(va, path=None):
    """Whether a function is a bare return - the mark of a message one side
    never sends."""
    path = path or os.path.join(CLIENT, 'Gamecode.dll')
    data = open(path, 'rb').read()
    off = _to_offset(path, va)
    if off is None:
        return None
    head = data[off:off + 4]
    return head[:1] == b'\xc3' or head[:1] == b'\xc2'


# Anchor: this one was walked by hand, instruction by instruction, and every
# step of it checked. Anything the automation produces is compared against it.
VERIFIED = {
    'SimpleCharFullUpdateIIR_t': {
        'factory': 0x1000CD00, 'constructor': 0x10078B84, 'vtable': 0x101624C0,
        'ReadSubClass': 0x1007916D, 'WriteSubClass': 0x10078010,
    },
}


def locate_all(path=None):
    """Every class name the client carries, located where possible.

    Results whose vtable is claimed by more than one class name are dropped.
    A vtable belongs to exactly one class, so a shared one means the search
    walked into a base class rather than the message - which is what happens to
    most of them, and is why this cannot be trusted one name at a time.
    """
    names = [n for n in client_class_names()]
    found = {}
    for name in names:
        try:
            hit = locate(name, path)
        except Exception:
            hit = None
        if hit:
            found[name] = hit

    owners = {}
    for name, hit in found.items():
        owners.setdefault(hit['vtable'], []).append(name)

    unique = {n: h for n, h in found.items() if len(owners[h['vtable']]) == 1}
    shared = {n: h for n, h in found.items() if len(owners[h['vtable']]) > 1}
    return unique, shared, [n for n in names if n not in found]


if __name__ == '__main__':
    if len(sys.argv) > 2 and sys.argv[1] == '--locate':
        for arg in sys.argv[2:]:
            found = locate(arg)
            if not found:
                print('%-34s not found' % arg)
                continue
            print('%s  (id 0x%08X)' % (arg, message_id(arg)))
            for key in ('string', 'factory', 'constructor', 'vtable', 'ReadSubClass', 'WriteSubClass'):
                print('   %-14s 0x%08X' % (key, found[key]))
    elif len(sys.argv) > 2 and sys.argv[1] == '--rtti':
        for arg in sys.argv[2:]:
            found = locate_rtti(arg)
            if not found:
                print('%-34s no RTTI record' % arg)
                continue
            print('%s  (id 0x%08X)' % (arg, message_id(arg)))
            for key in ('descriptor', 'locator', 'vtable', 'ReadSubClass', 'WriteSubClass'):
                mark = ''
                if key in ('ReadSubClass', 'WriteSubClass') and is_stub(found[key]):
                    mark = '   <- a bare return; this side never sends it'
                print('   %-14s 0x%08X%s' % (key, found[key], mark))
    elif len(sys.argv) > 1 and sys.argv[1] == '--locate-all':
        unique, shared, missing = locate_all()
        print('%d names: %d located uniquely, %d landed on a shared vtable, %d not found'
              % (len(unique) + len(shared) + len(missing), len(unique), len(shared), len(missing)))
        print('\nlocated (vtable claimed by this class alone):')
        for n in sorted(unique):
            h = unique[n]
            flag = ''
            if n in VERIFIED:
                flag = '  [matches hand-checked]' if all(VERIFIED[n][k] == h[k] for k in VERIFIED[n]) \
                    else '  [DISAGREES WITH HAND-CHECKED]'
            print('   %-34s Read 0x%08X  Write 0x%08X%s' % (n, h['ReadSubClass'], h['WriteSubClass'], flag))
        print('\ndropped - several names reached the same vtable, so the search')
        print('walked into a base class rather than the message itself:')
        for n in sorted(shared)[:8]:
            print('   %-34s vtable 0x%08X' % (n, shared[n]['vtable']))
        if len(shared) > 8:
            print('   ... and %d more' % (len(shared) - 8))
    elif len(sys.argv) > 1 and sys.argv[1] == '--audit':
        audit()
    elif len(sys.argv) > 1:
        for arg in sys.argv[1:]:
            print('0x%08X  %s' % (message_id(arg), arg))
    else:
        print(__doc__)
