"""Generate and verify the complete public SDK reference, then build its browsable site."""
from pathlib import Path
import re
import shutil
import subprocess
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[2]
sdk = root / 'docs/relevance/sdk'
projects = ['Mostlylucid.Relevance', 'Mostlylucid.Relevance.Sqlite', 'Mostlylucid.Relevance.Learning', 'Mostlylucid.Sqlite.Vector']

def run(*args):
    subprocess.run(args, cwd=root, check=True)

run('dotnet', 'tool', 'restore')
for project in projects:
    run('dotnet', 'build', project, '-c', 'Release')
run('dotnet', 'tool', 'run', 'docfx', 'metadata', str(sdk / 'docfx.json'), '--warningsAsErrors')
(sdk / 'api').mkdir(exist_ok=True)
shutil.copyfile(sdk / 'api-index.md', sdk / 'api/index.md')

expected_types = set()
for project in projects:
    for path in (root / project).glob('*.cs'):
        text = path.read_text()
        namespace = re.search(r'^namespace ([\w.]+);', text, re.M)
        if namespace:
            expected_types.update(namespace[1] + '.' + name for name in re.findall(
                r'^public\s+(?:(?:sealed|static|abstract|partial)\s+)*(?:class|record|interface|enum)\s+(\w+)', text, re.M))
ids = set()
for path in (sdk / 'api').glob('*.yml'):
    ids.update(value.strip("'\"") for value in re.findall(r'^  commentId: (.+)$', path.read_text(), re.M))
missing = ['T:' + name for name in expected_types if 'T:' + name not in ids]
# Protected ExecuteAsync is not a callable public member on the sealed worker.
nonpublic = {'M:Mostlylucid.Relevance.RelevanceWorker.ExecuteAsync(System.Threading.CancellationToken)'}
count = 0
for project in projects:
    document = ET.parse(root / project / f'bin/Release/net10.0/{project}.xml')
    for member in document.findall('./members/member'):
        name = member.attrib['name']
        if name in nonpublic or not any(name[2:] == t or name[2:].startswith(t + '.') for t in expected_types):
            continue
        count += 1
        if name not in ids:
            missing.append(name)
assert not missing, 'API reference omitted documented members:\n' + '\n'.join(sorted(set(missing)))
assert all((sdk / 'reference' / (name + '.md')).exists() for name in expected_types)
reference_index = '# Complete API reference\n\nGenerated from public declarations and XML documentation.\n\n'
reference_index += '\n'.join(f'- [{name}]({name}.md)' for name in sorted(expected_types)) + '\n'
(sdk / 'reference/index.md').write_text(reference_index)
run('dotnet', 'tool', 'run', 'docfx', 'build', str(sdk / 'docfx.json'), '--warningsAsErrors')
print(f'API coverage: {len(expected_types)} public types; {count} XML-documented declarations checked.')
