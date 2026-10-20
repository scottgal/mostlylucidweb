/**
 * Broken Link Rewriter Module
 * Fetches broken link mappings from the API and rewrites links on the page
 * Works in conjunction with BrokenLinkArchiveMiddleware for a belt-and-suspenders approach
 */

// Cache for link mappings to avoid repeated API calls
let linkMappingsCache = null;
let lastFetchTime = 0;
const CACHE_DURATION_MS = 5 * 60 * 1000; // 5 minutes

/**
 * Fetch broken link mappings from the API
 * @returns {Promise<Object>} Dictionary of originalUrl -> archiveUrl
 */
async function fetchLinkMappings() {
    // The archive mapping service is available only in database-backed mode.
    if (document.body.dataset.blogMode === 'File') return {};
    const now = Date.now();

    // Return cached data if still fresh
    if (linkMappingsCache && (now - lastFetchTime) < CACHE_DURATION_MS) {
        return linkMappingsCache;
    }

    try {
        const response = await fetch('/api/brokenlinks/mappings');
        if (!response.ok) {
            console.warn('Failed to fetch broken link mappings:', response.status);
            return {};
        }

        linkMappingsCache = await response.json();
        lastFetchTime = now;
        return linkMappingsCache;
    } catch (error) {
        console.warn('Error fetching broken link mappings:', error);
        return {};
    }
}

/**
 * Rewrite broken links in the given container
 * @param {Element} container - DOM element to search for links
 */
async function rewriteBrokenLinks(container = document) {
    const mappings = await fetchLinkMappings();

    if (Object.keys(mappings).length === 0) {
        return; // No mappings to apply
    }

    // Find all anchor tags
    const links = container.querySelectorAll('a[href]');
    let rewriteCount = 0;

    links.forEach(link => {
        const originalHref = link.getAttribute('href');

        // Skip if already processed or is an archive link
        if (link.hasAttribute('data-original-url')) return;
        if (originalHref.includes('archive.org')) return;

        // Check if this link has a mapping
        const archiveUrl = mappings[originalHref];
        if (archiveUrl) {
            // Store original URL as data attribute
            link.setAttribute('data-original-url', originalHref);
            link.setAttribute('href', archiveUrl);
            link.setAttribute('title', 'Original link unavailable - archived version');

            // Add visual indicator (optional - can be styled via CSS)
            link.classList.add('archived-link');

            rewriteCount++;
        }
    });

    if (rewriteCount > 0) {
        console.log(`Rewrote ${rewriteCount} broken links to archive.org URLs`);
    }
}

/**
 * Initialize broken link rewriting
 * Should be called on page load and after HTMX swaps
 */
function initBrokenLinks(container = document) {
    // Don't block - run in background
    rewriteBrokenLinks(container).catch(err => {
        console.warn('Error in broken link rewriter:', err);
    });
}

// Export functions for use in main.js
export { initBrokenLinks, rewriteBrokenLinks, fetchLinkMappings };
