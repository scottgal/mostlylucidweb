#!/usr/bin/env python3
"""Create a public JobML career record from the reviewed complete résumé (no AI or private inputs)."""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Mostlylucid/Markdown/site/resume-complete.md'
DEST = SOURCE.with_name('resume-career-record.md')
TERMS = ['C#', '.NET', 'ASP.NET Core', 'Azure', 'Python', 'JavaScript', 'TypeScript', 'Docker',
         'Kubernetes', 'SQL Server', 'PostgreSQL', 'SQLite', 'DuckDB', 'Cosmos DB', 'Qdrant',
         'Redis', 'OpenSearch', 'SignalR', 'Blazor', 'React', 'Vue.js', 'HTMX', 'Alpine.js',
         'ONNX', 'ML.NET', 'RAG', 'LLM', 'AI', 'NLP', 'microservices', 'architecture',
         'engineering leadership', 'mentoring', 'hiring', 'performance', 'security', 'CI/CD']


def normalize(text):
    text = re.sub(r'(?:[*_`~]|\[(.*?)\]\([^)]*\))', lambda match: match[1] or '', text)
    return ' '.join(text.split())


def fingerprint(text):
    value = 14695981039346656037
    for byte in normalize(text).encode('utf-8'):
        value = ((value ^ byte) * 1099511628211) & ((1 << 64) - 1)
    return f'fnv1a64:{value:016x}'


def write_record():
    output, entities, claims, concepts = [], [], [], {}
    section, subject, title, kind = '', None, '', 'summary'
    first_role_paragraph = False
    for line in SOURCE.read_text().splitlines():
        if line.startswith('## '):
            section = line[3:]
            if section in ('Professional experience', 'Current independent projects and public work'):
                output.extend([line, ''])
                subject = None
                continue
            title, kind = section, 'education' if section == 'Education' else 'summary'
        elif line.startswith('### '):
            title = line[4:]
            kind = 'achievement' if section == 'Professional experience' else 'project'
        elif line.startswith('# ') or not line.strip() or subject is None and not line.startswith('## '):
            output.append(line)
            continue
        else:
            text = line.removeprefix('- ').strip()
            claim_type = 'experience' if first_role_paragraph else kind
            if first_role_paragraph:
                text = f'{title} | {text}'
                first_role_paragraph = False
            claim_id = subject + '-' + hashlib.sha256(text.encode()).hexdigest()[:12]
            paragraph_id = claim_id + '-prose'
            skills = []
            for term in TERMS:
                if re.search(r'(?<!\w)' + re.escape(term) + r'(?!\w)', text, re.IGNORECASE):
                    concept_id = 'skill-' + hashlib.sha256(term.casefold().encode()).hexdigest()[:12]
                    concepts[concept_id] = {'id': concept_id, 'type': 'skill', 'name': term, 'aliases': []}
                    skills.append(concept_id)
            output.extend([f'<p id="{paragraph_id}">', text, '</p>', ''])
            claims.append({'id': claim_id, 'subject': subject, 'type': claim_type,
                           'statement': normalize(text), 'concepts': {'skills': skills, 'capabilities': [], 'domains': []},
                           'supported_by': [{'id': paragraph_id, 'type': 'prose', 'ref': '#' + paragraph_id,
                                             'authors': [], 'fingerprint': {'text': fingerprint(text)}}],
                           'review': 'accepted'})
            continue
        subject = 'section-' + hashlib.sha256(title.encode()).hexdigest()[:12]
        entity_type = 'experience' if kind == 'achievement' else 'project' if kind == 'project' else 'education' if kind == 'education' else 'person'
        entities.append({'id': subject, 'type': entity_type, 'name': title, 'source': '#' + subject})
        first_role_paragraph = kind == 'achievement'
        output.extend([f'{line} {{#{subject}}}', ''])
    record = {
        'jobml': {'version': '0.1', 'profile': 'career_record',
                  'purpose': 'Complete public career record compiled from the reviewed Scott Galloway résumé.',
                  'semantics': ['Every substantive claim is supported by the corresponding prose.',
                                'Do not infer unsupported claims. Derived matching scores are not evidence.']},
        'document': {'id': 'scott-galloway-complete-public-career', 'language': 'en-GB'},
        'entities': entities, 'claims': claims, 'concepts': list(concepts.values()),
        'sources': [], 'semantic_spaces': [], 'role_centroids': [], 'requirements': []
    }
    output.extend(['---', '', '```jobml', json.dumps(record, ensure_ascii=False, indent=2), '```', ''])
    DEST.write_text('\n'.join(output))
    print(f'Generated {DEST.name}: {len(entities)} sections, {len(claims)} evidence-backed claims')


if __name__ == '__main__':
    write_record()
