"""Build the tests, then run them - and refuse to report a stale result.

vstest.console.exe is happy to run yesterday's DLL. If the test project fails to
compile, the DLL on disk is whatever last succeeded, and the run prints a
cheerful "Passed!" for code that no longer exists. That has already happened
once in this repo: SimpleNpcInfo.Unknown1 was renamed, the test project stopped
compiling, and the run still said 114 passed.

So this builds first and stops on a build error, and it stops again if the DLL
is older than the newest source file under the messaging tree.

    python RunTests.py

Exit code is zero only when the build succeeded, the DLL is newer than every
source file, and every test passed.
"""
import os
import re
import subprocess
import sys
import time

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
MESSAGING = os.path.join(ROOT, 'OmniCell', 'Libraries', 'Source', 'AOtomation.Messaging')
PROJECT = os.path.join(MESSAGING, 'SmokeLounge.AOtomation.Messaging.Tests',
                       'SmokeLounge.AOtomation.Messaging.Tests.csproj')

# Where the build is told to put the DLL, rather than where it is guessed to
# have put it.
#
# The project says $(SolutionDir)\..\bin\test, and SolutionDir is only defined
# when MSBuild is given a solution - this gives it a project. So the path
# resolved from the drive root and the tests were being built to \bin\test at
# the root of whatever drive the repository was on, outside the repository
# entirely, which this script then had hard coded with a drive letter in it.
OUTPUT = os.path.join(ROOT, 'OmniCell', 'Built', 'test')
DLL = os.path.join(OUTPUT, 'Release', 'SmokeLounge.AOtomation.Messaging.Tests.dll')



def fail(message):
    print('')
    print('FAILED: ' + message)
    sys.exit(1)


# Visual Studio is wherever its installer put it: Community, Professional or
# Build Tools, 2022 or later, on whatever drive. vswhere.exe is installed with
# every one of them, always at this path, and is Microsoft's way of asking.
VSWHERE = os.path.join(os.environ.get('ProgramFiles(x86)', r'C:\Program Files (x86)'),
                       'Microsoft Visual Studio', 'Installer', 'vswhere.exe')


def find_in_visual_studio(pattern, requires=None):
    """The newest installed copy of a file, or None.

    -sort lists instances newest first, and -find returns only files that
    exist, so the first line is the newest instance that actually has it.
    """
    if not os.path.exists(VSWHERE):
        return None
    command = [VSWHERE, '-products', '*', '-sort', '-find', pattern]
    if requires:
        command[1:1] = ['-requires', requires]
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    out = process.communicate()[0].decode('utf-8', 'replace')
    for line in out.splitlines():
        if line.strip() and os.path.exists(line.strip()):
            return line.strip()
    return None


MSBUILD = find_in_visual_studio(r'MSBuild\**\Bin\MSBuild.exe', 'Microsoft.Component.MSBuild')
VSTEST = find_in_visual_studio(r'**\TestWindow\vstest.console.exe')
if not MSBUILD:
    fail('MSBuild not found. Install Visual Studio 2022 or its Build Tools - see SETUP.md')
if not VSTEST:
    fail('vstest.console.exe not found. Install the Visual Studio testing tools '
         '(the ".NET desktop development" workload, or "Testing tools core features" in Build Tools)')


def run(command):
    process = subprocess.Popen(command, stdout=subprocess.PIPE,
                               stderr=subprocess.STDOUT)
    out = process.communicate()[0].decode('utf-8', 'replace')
    return process.returncode, out


print('building ' + os.path.basename(PROJECT))
# -restore: the messaging library the tests reference is an SDK-style project,
# which will not build without its package assets.
#
# SolutionDir, not BaseIntermediateOutputPath: the test project derives its obj
# folder from $(SolutionDir), so given SolutionDir it lands in <repo>\obj\test.
# Overriding BaseIntermediateOutputPath instead applies to the messaging project
# too, and the two then shared one obj folder, restore assets included - which
# made NuGet treat the old-style test project as a package project and fail.
SOLUTION_DIR = os.path.join(ROOT, 'OmniCell') + os.sep
code, out = run([MSBUILD, PROJECT, '-restore', '-p:Configuration=Release', '-v:m', '-nologo',
                 '-p:SolutionDir=' + SOLUTION_DIR,
                 '-p:BaseOutputPath=' + OUTPUT + os.sep])
errors = [line for line in out.splitlines() if re.search(r'\berror\b', line, re.I)]
if code != 0 or errors:
    for line in errors[:20]:
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
code, out = run([VSTEST, DLL, '/Logger:console;verbosity=minimal'])
summary = [line for line in out.splitlines() if 'Total:' in line]
for line in summary:
    print('  ' + line.strip())
if code != 0 or not summary:
    for line in out.splitlines():
        if 'Failed ' in line or 'Assert' in line:
            print('  ' + line.strip())
    fail('tests did not pass')

count = re.search(r'Passed:\s*(\d+)', summary[0])
print('')
print('OK - %s tests passed against a DLL built from the current source'
      % (count.group(1) if count else '?'))
