// Custom service worker: network-first, same-origin only, with a bounded runtime cache.
//
// Wired up via PwaOptions.CustomServiceWorkerStrategyFileName in Program.cs. Strategy must be
// CustomStrategy or this file is ignored and the package's embedded worker is served instead.
// The package substitutes {version}, {offlineRoute} and {routes} before serving it at /serviceworker.
//
// Departures from the stock NetworkFirst strategy, each for a reason:
//
//   1. Cross-origin requests are not intercepted at all. They are no-cors/opaque, and routing them
//      through the cache broke every third-party image on the site. Badges are live counters and
//      must not be cached in any case.
//   2. HTMX partials are never cached. The same URL serves a full document or a layout-less
//      fragment depending on the HX-Request header, and the Cache API keys on URL alone - so
//      caching both would let a fragment be served as a whole page when the network is down.
//   3. Responses marked no-store/private are not cached, so admin and signed-in pages do not
//      linger on disk.
//   4. The runtime cache is capped. The stock worker grew without limit and was only ever cleared
//      by changing the version string, which never changed.

(function () {
    'use strict';

    // Update 'version' if you need to refresh the cache
    var version = '{version}';
    var offlineUrl = "{offlineRoute}";

    // Core cache: the offline page and any precached routes. Never trimmed.
    var CORE_CACHE = version + '::core';
    // Runtime cache: whatever visitors happen to browse. Trimmed to MAX_RUNTIME_ENTRIES.
    var RUNTIME_CACHE = version + '::runtime';
    var MAX_RUNTIME_ENTRIES = 60;

    function updateStaticCache() {
        return caches.open(CORE_CACHE)
            .then(function (cache) {
                return cache.addAll([
                    offlineUrl,
                    {routes}
                ]);
            });
    }

    // Cache API keys() returns insertion order, so dropping from the front evicts oldest first.
    function trimCache(cacheName, maxItems) {
        return caches.open(cacheName).then(function (cache) {
            return cache.keys().then(function (keys) {
                if (keys.length <= maxItems) return;
                return Promise.all(
                    keys.slice(0, keys.length - maxItems).map(function (key) {
                        return cache.delete(key);
                    })
                );
            });
        });
    }

    function isCacheable(request, response) {
        if (!response || !response.ok) return false;

        // Only plain same-origin responses. Opaque/cors responses have no business here.
        if (response.type !== 'basic' && response.type !== 'default') return false;

        // HTMX fragments share a URL with the full page - see note 2 above.
        if (request.headers.get('HX-Request')) return false;

        var cacheControl = response.headers.get('Cache-Control') || '';
        if (cacheControl.indexOf('no-store') !== -1 || cacheControl.indexOf('private') !== -1) return false;

        return true;
    }

    function addToCache(request, response) {
        if (!isCacheable(request, response)) return;

        var copy = response.clone();
        caches.open(RUNTIME_CACHE)
            .then(function (cache) {
                return cache.put(request, copy);
            })
            .then(function () {
                return trimCache(RUNTIME_CACHE, MAX_RUNTIME_ENTRIES);
            })
            .catch(function () {
                // A failed cache write must never affect the response the page receives.
            });
    }

    function fromCache(request) {
        return caches.match(request).then(function (response) {
            return response || caches.match(offlineUrl);
        });
    }

    self.addEventListener('install', function (event) {
        event.waitUntil(updateStaticCache().then(function () {
            return self.skipWaiting();
        }));
    });

    self.addEventListener('activate', function (event) {
        event.waitUntil(
            caches.keys()
                .then(function (keys) {
                    // Drop caches belonging to any previous version
                    return Promise.all(keys
                        .filter(function (key) {
                            return key.indexOf(version) !== 0;
                        })
                        .map(function (key) {
                            return caches.delete(key);
                        })
                    );
                })
                // Take over open tabs so visitors carrying an older worker are fixed on their
                // next page load rather than after a full browser restart.
                .then(function () {
                    return self.clients.claim();
                })
        );
    });

    self.addEventListener('fetch', function (event) {
        var request = event.request;

        // Never intercept cross-origin requests. Returning without calling respondWith() hands
        // the request back to the browser untouched, which is the only thing that works reliably
        // for opaque (no-cors) responses such as third-party images.
        var isSameOrigin;
        try {
            isSameOrigin = new URL(request.url).origin === self.location.origin;
        } catch (e) {
            isSameOrigin = false;
        }
        if (!isSameOrigin) return;

        // Always fetch non-GET requests from the network
        if (request.method !== 'GET') {
            event.respondWith(
                fetch(request).catch(function () {
                    return caches.match(offlineUrl);
                })
            );
            return;
        }

        event.respondWith(
            fetch(request)
                .then(function (response) {
                    addToCache(request, response);
                    return response;
                })
                .catch(function () {
                    return fromCache(request).then(function (response) {
                        if (response) return response;

                        var accept = request.headers.get('Accept') || '';
                        if (accept.indexOf('image') !== -1) {
                            return new Response('<svg role="img" aria-labelledby="offline-title" viewBox="0 0 400 300" xmlns="http://www.w3.org/2000/svg"><title id="offline-title">Offline</title><g fill="none" fill-rule="evenodd"><path fill="#D8D8D8" d="M0 0h400v300H0z"/><text fill="#9B9B9B" font-family="Helvetica Neue,Arial,Helvetica,sans-serif" font-size="72" font-weight="bold"><tspan x="93" y="172">offline</tspan></text></g></svg>', { headers: { 'Content-Type': 'image/svg+xml' } });
                        }

                        return Response.error();
                    });
                })
        );
    });

})();
