"""Apply audited Windows-only fixes to private copies of two locked dependencies."""
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import sys

source = Path(sys.argv[1]).resolve()
registry = Path(os.environ.get('CARGO_HOME', Path.home() / '.cargo')) / 'registry' / 'src'
patches = [
    ('sqlite3_ext', '0.2.0', 'src/connection.rs', '9d83aa9add21e9a3c513ba8101dc90802fbcb9a7efa9719a451aa4bcf8c8688f'),
    ('usearch', '2.24.0', 'include/usearch/index_plugins.hpp', 'a9ec023319dca3c69c55dd85a0041c162c5202a9c4085368a2ae5d3f055504bf'),
]
for name, version, relative, expected in patches:
    original = next(registry.glob(f'*/{name}-{version}'))
    if hashlib.sha256((original / relative).read_bytes()).hexdigest() != expected:
        raise SystemExit(f'Unexpected upstream source: {name} {version}; review before patching')
    destination = source / 'compat' / name
    if destination.exists():
        shutil.rmtree(destination)
    shutil.copytree(original, destination)

connection = source / 'compat/sqlite3_ext/src/connection.rs'
text = connection.read_text()
anchor = '#[cfg(unix)]\nfn path_to_cstring'
assert text.count(anchor) == 1
text = text.replace(anchor, '''#[cfg(windows)]
fn path_to_cstring(path: &Path) -> CString {
    CString::new(path.to_str().expect("SQLite filenames must be valid Unicode"))
        .expect("SQLite filenames cannot contain NUL")
}

''' + anchor)
connection.write_text(text)

allocator = source / 'compat/usearch/include/usearch/index_plugins.hpp'
text = allocator.read_text()
anchor = '            if (!new_arena || new_arena == (byte_t*)MAP_FAILED)'
assert text.count(anchor) == 1
text = text.replace(anchor, '''#if defined(USEARCH_DEFINED_WINDOWS)
            if (!new_arena)
#else
''' + anchor + '\n#endif')
allocator.write_text(text)

manifest = source / 'Cargo.toml'
text = manifest.read_text()
assert 'usearch = "2"' in text and '[patch.crates-io]' not in text
# The framework uses F32 vectors. Avoid unsupported MSVC half-precision SIMD intrinsics.
text = text.replace('usearch = "2"', 'usearch = { version = "2", default-features = false, features = ["fp16lib"] }')
text += '\n[patch.crates-io]\nusearch = { path = "compat/usearch" }\nsqlite3_ext = { path = "compat/sqlite3_ext" }\n'
manifest.write_text(text)

lock = source / 'Cargo.lock'
text = lock.read_text()
before = re.findall(r'name = "([^"\n]+)"\nversion = "([^"\n]+)"', text)
for name, version, _, _ in patches:
    pattern = (r'(\[\[package\]\]\nname = "' + name + r'"\nversion = "' + re.escape(version)
               + r'"\n)source = "registry[^"\n]+"\nchecksum = "[^"\n]+"\n')
    text, count = re.subn(pattern, r'\1', text)
    assert count == 1, f'Unexpected lock entry: {name}'
assert re.findall(r'name = "([^"\n]+)"\nversion = "([^"\n]+)"', text) == before
lock.write_text(text)
(source / 'windows-compatibility.json').write_text(json.dumps({
    'sqlite3_ext': '0.2.0: Windows UTF-8 path helper; invalid Unicode and NUL rejected',
    'usearch': '2.24.0: Windows allocator null check; optional SIMD kernels disabled; portable FP16 conversion retained',
    'upstream_checksums': {f'{name}/{relative}': expected for name, _, relative, expected in patches},
}, indent=2) + '\n')
print('Prepared private Windows compatibility dependencies; all locked versions preserved.')
