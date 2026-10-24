#!/usr/bin/env python3
"""Optional local LLM display-copy fixture. Requires a running Ollama and an already installed model.

Usage: python3 generate-copy.py catalogue.json copy.json [model] [count]
This never generates behaviour or outcome labels. Preserve the resulting JSON for exact replay.
"""
import argparse
import json
from pathlib import Path
import urllib.request

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('source', type=Path)
parser.add_argument('destination', type=Path)
parser.add_argument('model', nargs='?', default='qwen3.5:4b')
parser.add_argument('count', nargs='?', type=int, default=6)
args = parser.parse_args()
source, destination, model, count = args.source, args.destination, args.model, args.count
if destination.exists():
    raise SystemExit('Choose a new output path; keep previous generated fixtures for comparison.')
if not 1 <= count <= 24:
    raise SystemExit('Choose between 1 and 24 products per request.')
products = json.loads(source.read_text())[:count]
# Do not ask the model to reinterpret price/stock or to reproduce unconstrained fake marketing copy.
inputs = [{key: product[key] for key in ('id', 'name', 'category')} for product in products]
prompt = '''Write useful, plain display copy for these fictional demo products. Return JSON only:
{"products":[{"id":"existing-id","name":"short name","description":"short intended-use description"}]}
Use exactly the supplied IDs. Preserve each product kind and category. Do not invent materials,
size, dimensions, performance, certifications, health benefits, discounts or prices. No advertising
superlatives. Drop silly generated adjectives such as Tasty, Awesome or Intelligent from names.
Do not describe anything as electric, comfortable, high-quality, enhanced, compact or sustainable:
these attributes are not supplied. Describe purpose only: a flask carries drinks, a kettle heats
water, a speaker plays audio, boots are for walking. Invented short range names are fine. Do not mention a different kind of product. Names <= 80 characters, descriptions <= 200.
These are synthetic fixtures, not evidence about real products.
INPUT:
''' + json.dumps(inputs)
request = {'model': model, 'prompt': prompt, 'stream': False, 'think': False, 'format': 'json',
           'options': {'temperature': 0, 'seed': 814, 'num_predict': 1000}}
response = json.loads(urllib.request.urlopen(urllib.request.Request('http://127.0.0.1:11434/api/generate',
    data=json.dumps(request).encode(), headers={'Content-Type': 'application/json'}), timeout=180).read())
copy = json.loads(response['response'])
if set(copy) != {'products'} or len(copy['products']) != len(inputs) or {p['id'] for p in copy['products']} != {p['id'] for p in inputs}:
    raise SystemExit('Model output did not preserve the requested products.')
for product in copy['products']:
    if set(product) != {'id', 'name', 'description'} or not 1 <= len(product['name']) <= 80 or not 1 <= len(product['description']) <= 200:
        raise SystemExit('Model output did not meet the display-copy schema.')
# The host, not the LLM, supplies the model provenance and prompt version.
output = {'model': response['model'], 'promptVersion': 'catalogue-copy-v1', 'products': copy['products']}
destination.write_text(json.dumps(output, ensure_ascii=False, indent=2) + '\n')
destination.with_suffix('.prompt.txt').write_text(prompt)
destination.with_suffix('.provenance.json').write_text(json.dumps({
    'model': response['model'], 'createdAt': response.get('created_at'), 'seed': 814,
    'temperature': 0, 'promptVersion': 'catalogue-copy-v1', 'source': source.name,
    'note': 'Actual local inference; preserve the generated JSON to replay exact copy.'
}, indent=2) + '\n')
print('Generated display copy for', len(output['products']), 'products:', destination)
