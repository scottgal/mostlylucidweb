"""Fail early when a tracked filename cannot be checked out on Windows."""
from pathlib import PurePosixPath
import re
import subprocess

paths = subprocess.check_output(['git', 'ls-files', '-z']).decode('utf-8').split('\0')
invalid = []
for path in filter(None, paths):
    for part in PurePosixPath(path).parts:
        if (re.search(r'[<>:"\\|?*\x00-\x1f]', part) or part.endswith((' ', '.'))
                or re.fullmatch(r'(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?', part, re.I)):
            invalid.append(path)
            break
if invalid:
    raise SystemExit('Windows-incompatible tracked paths:\n' + '\n'.join(invalid))
print('Tracked paths are Windows-compatible.')
