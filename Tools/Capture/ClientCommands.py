r"""ClientCommands - every slash command the client knows, from the client.

    python ClientCommands.py
    python ClientCommands.py --csv commands.csv

Two questions, and they have different answers.

Which commands exist is answered by the strings. GUI.dll holds the command
words, and one that is never mentioned from code is a leftover rather than a
command, so this only counts a word whose address appears as an immediate
somewhere in the code section.

Which of them the client wires to a handler is answered by the registration
sequence, which is regular enough to read off the disassembly:

    push  <length>                  the name's length
    push  <address>                 the name, "/assist"
    call  0x10003810                std::string(name, length)
    push  <flags> <arity> <kind>
    call  0x100a3c6a                register, and hand back the command
    push  <address>                 the handler
    call  0x100bc2b3                attach it

Not every command is set up that way - several are named only inside arrays of
string pointers built on the stack, which is how the client groups them - so a
command with no handler here is one this reader could not follow, not one the
client ignores.

The help files the client ships, cd_image/text/help/*Commands*.html, name the
same commands in Funcom's own words and sort them into categories. Pass
--help-dir to fold in which ones are documented there; the text itself is
Funcom's and is not copied out.
"""
from __future__ import annotations

import argparse
import csv
import io
import os
import re
import struct
import sys

import capstone
import pefile

import MessageIds as mi

MODULES = ('GUI.dll', 'Gamecode.dll')

# The calls the registration sequences are built from, in GUI.dll.
#
# There are two shapes. One names the command with an explicit length and
# registers it on its own. The other builds a NULL terminated array of name
# pointers on the stack and walks it, registering every name in the array
# against the same handler - which is how the client gives one command several
# spellings, and why reading only the first shape finds a third of them.
MAKE_STRING = 0x10003810        # std::string(text, length)
MAKE_STRING_CSTR = 0x10003885   # std::string(text)
REGISTER = 0x100A3C6A

# A command word. Long enough to not be a fragment, short enough to be typed.
COMMAND = re.compile(rb'/[A-Za-z][A-Za-z0-9_]{1,20}\x00')


class Module(object):
    def __init__(self, path):
        self.path = path
        self.data = open(path, 'rb').read()
        self.pe = pefile.PE(data=self.data, fast_load=True)
        self.base = self.pe.OPTIONAL_HEADER.ImageBase
        self.spans = []
        for section in self.pe.sections:
            start = self.base + section.VirtualAddress
            size = max(section.Misc_VirtualSize, section.SizeOfRawData)
            self.spans.append((start, start + size, section.PointerToRawData,
                               section.Name.rstrip(b'\0').decode(errors='replace')))

    def to_va(self, offset):
        for start, end, raw, _ in self.spans:
            if raw <= offset < raw + (end - start):
                return start + (offset - raw)
        return None

    def read(self, va, count):
        for start, end, raw, _ in self.spans:
            if start <= va < end:
                off = raw + (va - start)
                return self.data[off:off + count]
        return b''

    def text_at(self, va, length):
        block = self.read(va, length)
        if len(block) != length or not all(32 <= c < 127 for c in block):
            return None
        return block.decode()

    def code(self):
        for start, end, raw, name in self.spans:
            if name == '.text':
                return start, self.data[raw:raw + (end - start)]
        return None, b''


def mentioned(module):
    """Command words in this module whose address appears in the code."""
    start, body = module.code()
    if not body:
        return {}

    words = {}
    for match in COMMAND.finditer(module.data):
        # The slash has to start the string, not sit inside one. Without this
        # every GUI path in the file - "Windows/Chat", "Default/Friends" -
        # reads as a command.
        if match.start() > 0 and module.data[match.start() - 1] != 0:
            continue
        va = module.to_va(match.start())
        if va is None:
            continue
        words[va] = match.group()[:-1].decode()

    seen = {}
    for va, word in words.items():
        if module.data.find(struct.pack('<I', va)) >= 0:
            # Only a mention from inside the code counts.
            at = body.find(struct.pack('<I', va))
            if at >= 0:
                seen[word] = va
    return seen


def registrations(module):
    """name -> handler, for every command the registration code sets up."""
    start, body = module.code()
    if not body or not (start <= REGISTER < start + len(body)):
        return {}

    def command_at(va):
        block = module.read(va, 24)
        end = block.find(bytes([0]))
        if end < 2:
            return None
        word = block[:end]
        if not word.startswith(b'/') or not all(32 <= c < 127 for c in word):
            return None
        return word.decode()

    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    md.detail = False

    # A linear walk stops dead at the first byte that is not an instruction,
    # and .text has plenty of those - jump tables, padding, alignment. So step
    # over whatever capstone refuses and carry on.
    def walk():
        at = 0
        while at < len(body):
            moved = False
            for one in md.disasm(body[at:], start + at):
                yield one
                at += one.size
                moved = True
            if not moved:
                at += 1

    code_end = start + len(body)

    found = {}
    pushes = []          # recent push immediates, newest last
    frame = {}           # stack slot -> the command name address stored in it
    loaded = None        # the last address loaded into a register
    pending = None       # names waiting for a handler
    awaiting = False     # register has run; the next code address is the handler

    slot = re.compile(r'^dword ptr \[ebp - (0x[0-9a-f]+)\], (0x[0-9a-f]+|[a-z]{2,3})$')

    for ins in walk():
        if ins.mnemonic == 'ret':
            # Slots are reused between functions, so do not carry them over.
            frame = {}
            pushes = []
            continue

        if ins.mnemonic == 'mov':
            match = slot.match(ins.op_str)
            if match:
                where = -int(match.group(1), 16)
                value = match.group(2)
                # A register store is the array's NULL terminator; the client
                # zeroes a register and writes it past the last name.
                frame[where] = int(value, 16) if value.startswith('0x') else 0
            elif re.fullmatch(r'e\w\w, (0x1[0-9a-f]{7})', ins.op_str):
                loaded = int(ins.op_str.split(', ')[1], 16)
            continue

        if ins.mnemonic == 'push':
            if re.fullmatch(r'-?(0x[0-9a-f]+|\d+)', ins.op_str):
                value = int(ins.op_str, 0)
                if awaiting and start <= value < code_end:
                    for name in pending or []:
                        found.setdefault(name, value)
                    pending, awaiting = None, False
                pushes.append(value)
                del pushes[:-6]
            continue

        if ins.mnemonic != 'call':
            continue

        target = int(ins.op_str, 0) if re.fullmatch(r'0x[0-9a-f]+', ins.op_str) else None
        if target == MAKE_STRING and len(pushes) >= 2:
            name = module.text_at(pushes[-1], pushes[-2])
            pending = [name] if name and name.startswith('/') else None
        elif target == MAKE_STRING_CSTR:
            # The name came in through a register, and it is the head of an
            # array if the slots after it hold command names too.
            head = loaded if loaded is not None else (pushes[-1] if pushes else None)
            pending = names_from(frame, head, command_at)
        elif target == REGISTER and pending:
            awaiting = True
        pushes = []
    return found


def names_from(frame, head, command_at):
    """The command names in the stack array that starts at this address."""
    name = command_at(head) if head else None
    if name is None:
        return None

    where = next((slot for slot, value in frame.items() if value == head), None)
    if where is None:
        return [name]

    names = []
    while True:
        value = frame.get(where)
        if not value:
            break
        word = command_at(value)
        if word is None:
            break
        names.append(word)
        where += 4
    return names or [name]


def imports(module):
    """IAT slot address -> a readable name, for the modules worth naming."""
    named = {}
    pe = pefile.PE(module.path)
    for entry in getattr(pe, 'DIRECTORY_ENTRY_IMPORT', []):
        dll = entry.dll.decode(errors='replace')
        if dll.lower() not in ('interfaces.dll', 'messageprotocol.dll',
                               'gamecode.dll', 'afcm.dll', 'connection.dll'):
            continue
        for item in entry.imports:
            if item.name:
                word = tidy(item.name.decode(errors='replace'))
                # Every module has one of these and naming them says nothing.
                if word in ('GetInstance', 'GetInstanceIfAny', 'Error',
                            'ErrorMessage'):
                    continue
                if dll.lower() != 'interfaces.dll':
                    word = dll.split('.')[0] + '::' + word
                named[item.address] = word
    return named


def tidy(symbol):
    """?N3Msg_LeaveTeam@N3InterfaceModule_t@@QBEXXZ -> N3Msg_LeaveTeam."""
    match = re.match(r'^\?\??(\w+)@(\w+)@@', symbol)
    if not match:
        return symbol
    return match.group(1)


def actions(module, handler, named, depth=0, seen=None):
    """The named engine calls a handler makes, directly or one call down.

    A command's handler is where it stops being a word and becomes something
    the client does, and Interfaces.dll's N3InterfaceModule_t is where the
    client does it - N3Msg_LeaveTeam, N3Msg_AssistFight, N3Msg_TextCommand.
    Those names say in the client's own words what a command is for, and
    whether it reaches the server at all.
    """
    start, body = module.code()
    if seen is None:
        seen = set()
    if handler in seen or depth < 0 or not (start <= handler < start + len(body)):
        return set()
    seen.add(handler)

    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    md.detail = False

    result = set()
    local = []
    at = handler - start
    steps = 0
    while at < len(body) and steps < 2000:
        moved = False
        for ins in md.disasm(body[at:], start + at):
            steps += 1
            at += ins.size
            moved = True
            if ins.mnemonic == 'call':
                target = ins.op_str
                slot = re.fullmatch(r'dword ptr \[(0x[0-9a-f]+)\]', target)
                if slot:
                    name = named.get(int(slot.group(1), 16))
                    if name:
                        result.add(name)
                elif re.fullmatch(r'0x[0-9a-f]+', target):
                    local.append(int(target, 16))
            if ins.mnemonic in ('ret', 'jmp') and ins.mnemonic == 'ret':
                at = len(body)
                break
            if steps >= 2000:
                break
        if not moved:
            at += 1

    for one in local:
        result |= actions(module, one, named, depth - 1, seen)
    return result


def documented(directory):
    """Which commands the client's own help files mention, and under what."""
    where = {}
    if not directory or not os.path.isdir(directory):
        return where

    # A command's own page names it first; a passing mention in a walkthrough
    # is worth less. So read the pages that are about commands first and let
    # the rest only fill gaps.
    entries = sorted(os.listdir(directory),
                     key=lambda e: (0 if 'command' in e.lower() else 1, e.lower()))
    for entry in entries:
        if not entry.lower().endswith('.html'):
            continue
        category = os.path.splitext(entry)[0]
        body = open(os.path.join(directory, entry), encoding='latin-1').read()
        # Strip the links, which are all file:// and href= paths rather than
        # commands, before looking for command words.
        body = re.sub(r'<a[^>]*>', ' ', body)
        body = body.replace('file://', ' ')
        for word in re.findall(r'/[A-Za-z][A-Za-z0-9_]{1,20}', body):
            where.setdefault(word.lower(), category)
    return where


def channel(row):
    """Where a command goes, as far as its handler can be followed."""
    if not row['handler']:
        return 'unfollowed'
    engine = [c for c in row['does'] if not c.startswith(('AFCM::', 'Connection::'))]
    if engine:
        return 'zone'
    if any(c.startswith('AFCM::') for c in row['does']):
        return 'bus'
    return 'gui'


CHANNELS = (
    ('zone', 'Reaches the game',
     "The handler calls into Interfaces.dll's N3InterfaceModule_t, which is the "
     "client's whole client-to-server action surface. The call named on each row "
     "is what the command actually asks for. N3Msg_TextCommand is the general "
     "one - the commands that share it are the ones the server parses as text "
     "rather than as their own message."),
    ('bus', 'Goes on the client bus',
     'The handler hands the command to AFCM, the client\'s own message bus, and '
     'what happens next is a matter for whichever part of the client subscribed.'),
    ('gui', 'Handled inside the GUI',
     'The handler calls nothing outside GUI.dll. Some of these are purely local - '
     "a window toggle, a macro - and the rest are the chat commands, which go out "
     'on the chat connection GUI.dll owns rather than through the game interface. '
     'Either way the zone server never sees them.'),
    ('unfollowed', 'Registration not followed',
     'The word is in the client and the code mentions it, but this reader could '
     'not find where it is registered. Absence here is a limit of the reader, not '
     'a statement about the client.'),
)


def escape(text):
    return (text.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;'))


def write_html(rows, handler_names, path):
    """The commands page, in the same shape as the protocol index."""
    parts = []
    add = parts.append
    add('<!doctype html><html lang="en"><head><meta charset="utf-8">'
        '<meta name="viewport" content="width=device-width,initial-scale=1">'
        '<title>OmniCell AO Client Commands</title>'
        '<link rel="stylesheet" href="protocol.css"></head><body>')

    counted = {}
    for row in rows.values():
        key = channel(row)
        counted[key] = counted.get(key, 0) + 1

    add('<header><h1>OmniCell AO Client Commands</h1><div class="sub">'
        + str(len(rows)) + ' commands this client knows &middot; '
        + str(counted.get('zone', 0)) + ' reach the game &middot; '
        + str(counted.get('gui', 0) + counted.get('bus', 0))
        + ' never leave the client</div></header><main>')
    add('<a class="back" href="index.html">&larr; protocol index</a>')

    add('<div class="panel"><h2>Where this comes from</h2><p>'
        'Every word here is the client\'s, not ours. GUI.dll holds the command '
        'strings, and a word is only counted when the code mentions its address, '
        'so a leftover string is not a command. The handler column is the routine '
        'the client attaches to that word; where several commands share one '
        'handler the client has registered them together, and the handler decides '
        'between them by the word it was given.'
        '</p><p>'
        'The last column is what the handler calls, and it is the useful one. '
        'Interfaces.dll exports N3InterfaceModule_t - about two hundred and fifty '
        'named functions that are the client\'s entire client-to-server surface - '
        'so a command that calls one of those reaches the game, and the name says '
        'what it asks for. Only direct calls are followed: going one level deeper '
        'sweeps up the helpers every handler shares and the answer stops meaning '
        'anything.'
        '</p><p>'
        'The category is the client\'s own, from the help files it ships in '
        'cd_image/text/help. A command with no category there is usually one for '
        'Funcom rather than for players - /spawn, /teleport, /monsterdata, '
        '/dumphash - and a live server would refuse it anyway.'
        '</p><p>'
        'One row is worth reading on its own. /anim calls N3Msg_GetActionByName '
        'and then N3Msg_DoSocialAction, which is the client resolving an emote '
        'word against the table of seventy that Gamecode.dll and GUI.dll both '
        'carry, and then performing it - the same table SocialAction is now '
        'generated from.'
        '</p></div>')

    for key, title, blurb in CHANNELS:
        picked = sorted(word for word, row in rows.items() if channel(row) == key)
        if not picked:
            continue
        add('<div class="panel"><h2>' + title + ' (' + str(len(picked)) + ')</h2>')
        add('<p class="sub">' + blurb + '</p>')
        add('<table><tr><th>command</th><th>handler</th><th>registered with</th>'
            '<th>the client files it under</th><th>what the handler calls</th></tr>')
        # Sorted by handler so commands registered together sit together, and
        # the group is visible without repeating its members on every row.
        picked.sort(key=lambda w: (rows[w]['handler'], w))
        for word in picked:
            row = rows[word]
            handler = ('0x%08X' % row['handler']) if row['handler'] else ''
            group = len(handler_names.get(row['handler'], []))
            add('<tr><td>' + escape(word) + '</td><td>' + handler + '</td><td>'
                + (str(group) + ' commands' if group > 1 else '') + '</td><td>'
                + escape(row['help']) + '</td><td>'
                + escape(', '.join(row['does'])) + '</td></tr>')
        add('</table></div>')

    add('</main></body></html>')
    io.open(path, 'w', encoding='utf-8', newline='').write(''.join(parts))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--csv', help='write the list here as well')
    parser.add_argument('--html', help='write the commands page here')
    parser.add_argument('--help-dir', default=os.path.join(mi.CLIENT, 'cd_image', 'text', 'help'),
                        help="the client's own help files")
    args = parser.parse_args()

    help_index = documented(args.help_dir)

    rows = {}
    handler_names = {}
    for name in MODULES:
        path = os.path.join(mi.CLIENT, name)
        if not os.path.exists(path):
            continue
        module = Module(path)
        handlers = registrations(module)
        named = imports(module)
        cache = {}
        for word in mentioned(module):
            row = rows.setdefault(
                word, {'module': name, 'handler': 0, 'help': '', 'does': []})
            if word in handlers:
                row['handler'] = handlers[word]
                where = row['handler']
                handler_names.setdefault(where, []).append(word)
                if where not in cache:
                    cache[where] = sorted(actions(module, where, named))
                row['does'] = cache[where]
            row['help'] = help_index.get(word.lower(), '')

    print('%-22s %-12s %-24s %s' % ('command', 'handler', 'documented under', 'what it calls'))
    for word in sorted(rows):
        row = rows[word]
        print('%-22s %-12s %-24s %s' % (
            word,
            ('0x%08X' % row['handler']) if row['handler'] else '',
            row['help'],
            ', '.join(row['does'][:4])))

    wired = sum(1 for r in rows.values() if r['handler'])
    inhelp = sum(1 for r in rows.values() if r['help'])
    reaches = sum(1 for r in rows.values() if r['does'])
    print()
    print('%d commands, %d with a handler this reader could follow, %d of those reaching'
          ' a named engine call, %d in the help files'
          % (len(rows), wired, reaches, inhelp))

    if args.html:
        write_html(rows, handler_names, args.html)
        print('written to ' + args.html)

    if args.csv:
        with open(args.csv, 'w', newline='') as handle:
            out = csv.writer(handle)
            out.writerow(['command', 'module', 'handler', 'help', 'calls'])
            for word in sorted(rows):
                row = rows[word]
                out.writerow([word, row['module'],
                              ('0x%08X' % row['handler']) if row['handler'] else '',
                              row['help'], ' '.join(row['does'])])
        print('written to ' + args.csv)
    return 0


if __name__ == '__main__':
    sys.exit(main())
