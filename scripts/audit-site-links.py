#!/usr/bin/env python3
"""Audit every href exposed by the primary site pages. Uses GET, since several routes reject HEAD.
Example: python3 scripts/audit-site-links.py --base http://localhost:8080 --report /tmp/links.json
Add --article-content to include links from every article reachable from listing pages.
"""
import argparse
import concurrent.futures
import json
from html.parser import HTMLParser
from pathlib import Path
import time
import urllib.error
import urllib.parse
import urllib.request


class Links(HTMLParser):
    def __init__(self):
        super().__init__()
        self.hrefs = set()
        self.ids = set()
        self.stack = []

    def handle_starttag(self, tag, attrs):
        values = dict(attrs)
        if values.get('id'):
            self.ids.add(values['id'])
        suppressed = any(item[1] for item in self.stack) or tag == 'article' or 'data-published-date' in values
        if tag == 'a' and not suppressed and (values.get('href') or values.get('hx-get')):
            href = values.get('href') or values['hx-get']
            # Cloudflare replaces email links with this browser-decoded placeholder.
            if not href.startswith('/cdn-cgi/l/email-protection'):
                self.hrefs.add(href)
        if tag not in ('area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'param', 'source', 'track', 'wbr'):
            self.stack.append((tag, suppressed))

    def handle_endtag(self, tag):
        for index in range(len(self.stack) - 1, -1, -1):
            if self.stack[index][0] == tag:
                del self.stack[index:]
                break


def fetch(url):
    try:
        request = urllib.request.Request(url, headers={'User-Agent': 'mostlylucid-link-audit/1.0'})
        with urllib.request.urlopen(request, timeout=15) as response:
            html = response.read(8_000_000).decode('utf-8', errors='replace') if 'text/html' in response.headers.get('Content-Type', '') else ''
            return response.status, response.url, html, None
    except urllib.error.HTTPError as error:
        return error.code, error.url, '', str(error)
    except Exception as error:
        return 0, url, '', str(error)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base', required=True)
    parser.add_argument('--report', required=True)
    parser.add_argument('--article-content', action='store_true')
    args = parser.parse_args()
    base = args.base.rstrip('/')
    origin = urllib.parse.urlsplit(base).netloc
    seeds = ['/', '/blog?pageSize=100', '/software', '/contact', '/resumes', '/resume', '/projects/activity', '/search?query=bot']
    checks = {base + path: {'sources': ['seed']} for path in seeds}
    pending = list(checks)
    visited = set()
    failures = []
    start = time.time()
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        while pending:
            batch = pending
            pending = []
            for url, (status, final, html, error) in zip(batch, pool.map(fetch, batch)):
                entry = checks[url]
                entry.update(status=status, final_url=final)
                if error:
                    entry['detail'] = error
                if status == 0 or status in (401, 403, 405, 429):
                    entry['result'] = 'manual_check'
                elif status >= 400:
                    entry['result'] = 'broken'
                    failures.append(url)
                else:
                    entry['result'] = 'ok'
                if not html or url in visited:
                    continue
                visited.add(url)
                parsed = urllib.parse.urlsplit(url)
                query = urllib.parse.parse_qs(parsed.query)
                # Crawl primary pages and all listing pagination. Article contents are opt-in.
                crawl = (parsed.netloc == origin and (url in [base + path for path in seeds]
                    or args.article_content and parsed.path.startswith('/blog/')))
                if not crawl:
                    continue
                links = Links()
                links.feed(html)
                for href in sorted(links.hrefs):
                    target = urllib.parse.urljoin(final, href)
                    parts = urllib.parse.urlsplit(target)
                    if parts.scheme not in ('http', 'https'):
                        continue
                    target = urllib.parse.urldefrag(target)[0]
                    if target in checks:
                        checks[target]['sources'].append(url)
                        continue
                    checks[target] = {'sources': [url]}
                    pending.append(target)
            print(f'Checked {sum("status" in item for item in checks.values())}; queued {len(pending)}', flush=True)
    report = {
        'base': base, 'article_content': args.article_content,
        'scope': 'Navigation hrefs from primary pages (article body links excluded)',
        'elapsed_seconds': round(time.time() - start, 1), 'checked': len(checks),
        'broken': failures,
        'manual_check': [url for url, item in checks.items() if item.get('result') == 'manual_check'],
        'links': checks
    }
    Path(args.report).write_text(json.dumps(report, indent=2))
    print(json.dumps({key: value for key, value in report.items() if key != 'links'}, indent=2))
    return bool(failures)


if __name__ == '__main__':
    raise SystemExit(main())
