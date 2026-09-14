"""Build the tests, then run them - and refuse to report a stale result.

A test runner is happy to run yesterday's DLL. If the test project fails to
compile, the DLL on disk is whatever last succeeded, and the run prints a
cheerful "Passed!" for code that no longer exists. That has already happened
once in this repo: SimpleNpcInfo.Unknown1 was renamed, the test project stopped
compiling, and the run still said 114 passed.

So this builds first and stops on a build error, and it stops again if the DLL
is older than the newest source file under the messaging tree.

    python RunTests.py

It needs only the .NET 10 SDK - the dotnet command - so it runs the same way
wherever that is installed. Exit code is zero only when the build succeeded,
the DLL is newer than every source file, and every test passed.
"""
import os
import re
import shutil
import subprocess
import sys
import time

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
MESSAGING = os.path.join(ROOT, 'OmniCell', 'Libraries', 'Source', 'AOtomation.Messaging')
PROJECT = os.path.join(MESSAGING, 'SmokeLounge.AOtomation.Messaging.Tests',
                       'SmokeLounge.AOtomation.Messaging.Tests.csproj')

# Where the build is told to put the DLL, rather than where it is guessed to
# have put it.
OUTPUT = os.path.join(ROOT, 'OmniCell', 'Built', 'test')
DLL = os.path.join(OUTPUT, 'Release', 'net10.0', 'SmokeLounge.AOtomation.Messaging.Tests.dll')


def fail(message):
    print('')
    print('FAILED: ' + message)
    sys.exit(1)


DOTNET = shutil.which('dotnet')
if not DOTNET:
    fail('dotnet not found. Install the .NET 10 SDK - see SETUP.md')


def run(command):
    # From the repository root, so global.json picks the .NET 10 SDK.
    process = subprocess.Popen(command, stdout=subprocess.PIPE,
                               stderr=subprocess.STDOUT, cwd=ROOT)
    out = process.communicate()[0].decode('utf-8', 'replace')
    return process.returncode, out


print('building ' + os.path.basename(PROJECT))
code, out = run([DOTNET, 'build', PROJECT, '-c', 'Release', '-v:m', '-nologo',
                 '-p:BaseOutputPath=' + OUTPUT + os.sep])
# MSBuild writes compile errors as "file(line,column): error CODE: text".
errors = [line for line in out.splitlines() if re.search(r':\s*error\s+[A-Z]+\d+', line)]
if code != 0 or errors:
    for line in (errors or out.splitlines())[:20]:
        print('  ' + line.strip())
    fail('the tests did not compile, so any run would be the previous DLL')

if not os.path.exists(DLL):
    fail('no test DLL at ' + DLL)

newest, newest_path = 0, None
for base, _, names in os.walk(MESSAGING):
    if os.sep + 'bin' in base or os.sep + 'obj' in base:
        continue
    for name in names:
        if not name.endswith('.cs'):
            continue
        path = os.path.join(base, name)
        stamp = os.path.getmtime(path)
        if stamp > newest:
            newest, newest_path = stamp, path

built = os.path.getmtime(DLL)
if newest > built:
    print('  DLL   ' + time.strftime('%H:%M:%S', time.localtime(built)))
    print('  newer ' + time.strftime('%H:%M:%S', time.localtime(newest)) +
          '  ' + os.path.relpath(newest_path, ROOT))
    fail('the DLL is older than a source file, so the run would be stale')

print('running')
code, out = run([DOTNET, 'test', DLL, '--logger', 'console;verbosity=minimal'])
summary = [line for line in out.splitlines() if 'Total:' in line]
for line in summary:
    print('  ' + line.strip())
if code != 0 or not summary:
    for line in out.splitlines():
        if 'Failed ' in line or 'Assert' in line or 'error' in line.lower():
            print('  ' + line.strip())
    fail('tests did not pass')

count = re.search(r'Passed:\s*(\d+)', summary[0])
print('')
print('OK - %s tests passed against a DLL built from the current source'
      % (count.group(1) if count else '?'))
