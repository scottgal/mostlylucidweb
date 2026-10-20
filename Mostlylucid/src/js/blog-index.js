// GET forms serialize every active filter. Search, filters, reset and pagination
// share hx-sync="#contentcontainer:replace" so older responses cannot undo newer choices.
export function syncFilters(root = document) {
    const form = root.querySelector('[data-blog-filters]');
    if (!form) return;
    const url = new URL(window.location.href);
    const searchPage = url.pathname.startsWith('/search');
    const defaults = { language: searchPage ? '' : 'en', category: '', startDate: '', endDate: '', order: 'date_desc' };
    for (const [name, fallback] of Object.entries(defaults)) {
        const control = form.elements.namedItem(name);
        if (!control) continue;
        const legacyOrder = name === 'order' && url.searchParams.has('orderBy')
            ? `${url.searchParams.get('orderBy')}_${url.searchParams.get('orderDir') || 'desc'}` : null;
        control.value = url.searchParams.get(name) ?? legacyOrder ?? fallback;
    }
    const query = form.querySelector('[data-filter-query]');
    if (query) query.value = url.searchParams.get('query') || '';
    const pageSize = form.elements.namedItem('pageSize');
    if (pageSize && url.searchParams.has('pageSize')) pageSize.value = url.searchParams.get('pageSize');
    const start = form.elements.namedItem('startDate');
    const end = form.elements.namedItem('endDate');
    start.max = end.value;
    end.min = start.value;
    const summary = document.querySelector('#filterSummary');
    if (summary) {
        const language = form.elements.namedItem('language');
        const category = form.elements.namedItem('category');
        const order = form.elements.namedItem('order');
        summary.textContent = [
            language.selectedOptions[0]?.textContent,
            category.value || 'All topics',
            order.selectedOptions[0]?.textContent,
            start.value || end.value ? `${start.value || 'Any date'} → ${end.value || 'Today'}` : null
        ].filter(Boolean).join(' · ');
    }
}

export function prepareFilterRequest(event) {
    const form = event.detail.elt?.closest('[data-blog-filters]');
    if (!form) return;
    const query = document.querySelector('#searchQuery');
    if (query && form.querySelector('[data-filter-query]')) {
        // Auto-search may have updated the visible query since this form rendered.
        event.detail.parameters.query = query.value;
    }
    // Empty dates must be absent: ASP.NET's nullable date binding expects no value.
    for (const name of ['startDate', 'endDate']) {
        if (!event.detail.parameters[name]) delete event.detail.parameters[name];
    }
}

if (!window.__blogIndexBound) {
    window.__blogIndexBound = true;
    document.addEventListener('htmx:configRequest', prepareFilterRequest);
    document.addEventListener('change', event => {
        const form = event.target.closest('[data-blog-filters]');
        if (!form) return;
        const start = form.elements.namedItem('startDate');
        const end = form.elements.namedItem('endDate');
        start.max = end.value;
        end.min = start.value;
    });
    for (const name of ['htmx:pushedIntoHistory', 'htmx:historyRestore', 'htmx:afterSettle']) {
        document.addEventListener(name, () => syncFilters());
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => syncFilters());
    else setTimeout(() => syncFilters(), 0);
}
window.blogIndexInit = syncFilters;
