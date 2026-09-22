r"""BuildDocs - run BuildProtocolDocs.ps1 and refuse to be ignored.

    python BuildDocs.py

The generator is a PowerShell script whose packet write-ups are single-quoted
strings, so an apostrophe inside one has to be doubled. Miss that and the whole
file fails to parse - and the failure is quiet in the way that matters: the
script prints a parse error, writes nothing, and the previous run's pages stay
on disk looking perfectly fine. Commit at that point and the repository claims
a write-up it does not have. That happened twice on 2026-09-11.

There is a second way to break it that looks nothing like the first. The file
is UTF-8 with a byte order mark, and the write-ups contain arrows and other
non-ASCII; strip the mark - which an editing script does simply by writing the
file back without one - and PowerShell 5.1 reads the whole thing as ANSI, the
arrow in `Direction='client → server'` becomes three characters, one of them a
quote, and the parse dies hundreds of characters away from anything you
changed. That happened on 2026-09-12. So the mark is checked before the run and
restored if it has gone.

This runs the generator, checks the success line is actually there, and exits
non-zero when it is not. It also counts the green packets, because a number
that moved when nothing should have moved it is the other thing worth seeing
before a commit.

Use it instead of calling the .ps1 directly.
"""
from __future__ import annotations

import os
import re
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SCRIPT = os.path.join(HERE, 'BuildProtocolDocs.ps1')
INDEX = os.path.join(HERE, '..', '..', 'OmniCell', 'Documentation', 'protocol', 'index.html')
SUCCESS = 'Generated protocol index and'
BOM = bytes([0xEF, 0xBB, 0xBF])


def ensure_bom():
    """Put the byte order mark back if an editing script dropped it.

    Without it PowerShell 5.1 reads the file as ANSI and every non-ASCII
    character in a write-up turns into mojibake, which fails the parse in a
    place that has nothing to do with the edit that caused it.
    """
    raw = open(SCRIPT, 'rb').read()
    if raw.startswith(BOM):
        return False
    open(SCRIPT, 'wb').write(BOM + raw)
    return True


def main():
    if ensure_bom():
        print('the byte order mark had gone from BuildProtocolDocs.ps1 - put back')
    run = subprocess.run(
        ['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', SCRIPT],
        capture_output=True, text=True)
    output = (run.stdout or '') + (run.stderr or '')

    if SUCCESS not in output:
        sys.stdout.write(output)
        print()
        print('BuildProtocolDocs.ps1 did not finish. The pages on disk are from an')
        print('earlier run and do not match the source - do not commit them.')
        print()
        print('The usual cause is an apostrophe in a write-up. The strings are')
        print('single-quoted PowerShell, so it has to be doubled:')
        print("    'the client''s own reader'   not   'the client's own reader'")
        raise SystemExit(1)

    print(output.strip().splitlines()[-1])
    try:
        with open(INDEX, encoding='utf-8') as handle:
            green = len(re.findall(r'complete">every byte identified', handle.read()))
        print('%d packets with every byte identified' % green)
    except OSError:
        pass


if __name__ == '__main__':
    main()
