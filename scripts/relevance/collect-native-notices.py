"""Retain dependency SPDX declarations and available license/notice texts from the locked native build."""
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

source, destination = map(Path, sys.argv[1:3])
destination.mkdir(parents=True, exist_ok=True)
registry = Path(os.environ.get('CARGO_HOME', Path.home() / '.cargo')) / 'registry' / 'src'
tree = subprocess.check_output(['cargo', 'tree', '--manifest-path', str(source / 'Cargo.toml'), '--locked', '--edges', 'normal,build', '--prefix', 'none', '--format', '{p}'], text=True)
entries = []
for name, version in sorted(set(re.findall(r'^([\w-]+) v([\w.+-]+)', tree, re.M))):
    roots = [source] if name == 'sqlite-vector-rs' else list(registry.glob(f'*/{name}-{version}'))
    if not roots:
        raise SystemExit(f'Dependency source missing: {name} {version}')
    root = roots[0]
    text = (root / 'Cargo.toml').read_text()
    manifest = dict(re.findall(r'^([a-z_]+) = \"([^\"]*)\"', text, re.M))
    notices = sorted({p for pattern in ('LICENSE*', 'NOTICE*', 'COPYING*') for p in root.rglob(pattern) if p.is_file() and 'target' not in p.parts})
    target = destination / f'{name}-{version}'
    target.mkdir(exist_ok=True)
    for i, notice in enumerate(notices):
        shutil.copyfile(notice, target / f'{i}-{notice.name}')
    entries.append({'name': name, 'version': version, 'license': manifest.get('license'), 'repository': manifest.get('repository'), 'notice_files': len(notices)})
(destination / 'dependencies.json').write_text(json.dumps(entries, indent=2) + '\n')
print(f'Retained SPDX metadata for {len(entries)} locked build dependencies')
