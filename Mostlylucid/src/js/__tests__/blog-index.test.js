import { beforeEach, describe, expect, it } from 'vitest';
import { syncFilters, prepareFilterRequest } from '../blog-index';

beforeEach(() => {
    window.history.replaceState({}, '', '/');
    document.body.innerHTML = `
        <form data-blog-filters>
            <select name="category"><option value="">All topics</option><option value="C#">C#</option></select>
            <select name="language"><option value="">All languages</option><option value="en">English</option><option value="es">Spanish</option></select>
            <input type="date" name="startDate"><input type="date" name="endDate">
            <select name="order"><option value="relevance">Best match</option><option value="date_desc">Newest first</option><option value="title_asc">Title A–Z</option></select>
            <input name="pageSize" value="10"><input name="query" data-filter-query>
        </form>
        <p id="filterSummary"></p>`;
});

const control = name => document.querySelector(`[name="${name}"]`);

describe('article filter state', () => {
    it('restores all filters from a shared URL and preserves the selected sort', () => {
        window.history.replaceState({}, '', '/?category=C%23&language=es&order=title_asc&startDate=2026-01-01&endDate=2026-10-19&pageSize=20');
        syncFilters();
        expect(control('category').value).toBe('C#');
        expect(control('language').value).toBe('es');
        expect(control('order').value).toBe('title_asc');
        expect(control('startDate').value).toBe('2026-01-01');
        expect(control('endDate').min).toBe('2026-01-01');
        expect(control('startDate').max).toBe('2026-10-19');
        expect(control('pageSize').value).toBe('20');
        expect(document.querySelector('#filterSummary').textContent).toContain('Title A–Z');
    });

    it('resets controls and date constraints after reset or history restoration', () => {
        window.history.replaceState({}, '', '/?language=es&category=C%23&startDate=2026-01-01');
        syncFilters();
        window.history.replaceState({}, '', '/');
        syncFilters();
        expect(control('language').value).toBe('en');
        expect(control('category').value).toBe('');
        expect(control('startDate').value).toBe('');
        expect(control('endDate').min).toBe('');
        expect(control('order').value).toBe('date_desc');
    });

    it('defaults search to newest first and all languages and retains the search query', () => {
        window.history.replaceState({}, '', '/search?query=dotnet');
        syncFilters();
        expect(control('order').value).toBe('date_desc');
        expect(control('language').value).toBe('');
        expect(control('query').value).toBe('dotnet');
    });

    it('reads legacy sort URLs', () => {
        window.history.replaceState({}, '', '/?orderBy=title&orderDir=asc');
        syncFilters();
        expect(control('order').value).toBe('title_asc');
    });

    it('keeps the visible search query and every active filter in a request', () => {
        document.body.insertAdjacentHTML('beforeend', '<input id="searchQuery" value="updated search">');
        const parameters = { query: 'old search', category: 'C#', language: 'es', order: 'title_asc', startDate: '2026-01-01', endDate: '', page: '1' };
        prepareFilterRequest({ detail: { elt: control('order'), parameters } });
        expect(parameters).toEqual({ query: 'updated search', category: 'C#', language: 'es', order: 'title_asc', startDate: '2026-01-01', page: '1' });
    });

    it('does not alter unrelated HTMX requests', () => {
        const elt = document.createElement('button');
        const parameters = { startDate: '', query: 'untouched' };
        prepareFilterRequest({ detail: { elt, parameters } });
        expect(parameters).toEqual({ startDate: '', query: 'untouched' });
    });
});
