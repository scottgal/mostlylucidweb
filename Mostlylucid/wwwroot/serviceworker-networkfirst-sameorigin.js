// Custom service worker: NetworkFirst, but same-origin only.
//
// Wired up via PwaOptions.CustomServiceWorkerStrategyFileName in Program.cs. The package
// substitutes {version}, {offlineRoute} and {routes} before serving this at /serviceworker.
//
// Why this exists: the stock NetworkFirst strategy calls event.respondWith() for every GET,
// including cross-origin ones. Cross-origin <img> requests are no-cors and produce opaque
// responses, and routing those through the cache broke every external image on the site -
// shields.io badges, GitHub avatars, anything third-party. Letting the browser handle
// cross-origin requests natively is both correct and what we want anyway: download-count
// badges are live data and must never be cached.
(function () {
    'use strict';

    // Update 'version' if you need to refresh the cache
    var version = '{version}';
    var offlineUrl = "{offlineRoute}";

    // Store core files in a cache (including a page to display when offline)
    function updateStaticCache() {
        return caches.open(version)
            .then(function (cache) {
                return cache.addAll([
                    offlineUrl,
                    {routes}
                ]);
            });
    }

    function addToCache(request, response) {
        if (!response.ok)
            return;

        var copy = response.clone();
        caches.open(version)
            .then(function (cache) {
                cache.put(request, copy);
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
                    // Remove caches whose name is no longer valid
                    return Promise.all(keys
                        .filter(function (key) {
                            return key.indexOf(version) !== 0;
                        })
                        .map(function (key) {
                            return caches.delete(key);
                        })
                    );
                })
                // Take over existing tabs so visitors carrying the old broken worker are
                // fixed on their next page load rather than after a full browser restart.
                .then(function () {
                    return self.clients.claim();
                })
        );
    });

    self.addEventListener('fetch', function (event) {
        var request = event.request;

        // Never intercept cross-origin requests. Returning without calling respondWith()
        // hands the request back to the browser untouched, which is the only thing that
        // works reliably for opaque (no-cors) responses like third-party images.
        var isSameOrigin;
        try {
            isSameOrigin = new URL(request.url).origin === self.location.origin;
        } catch (e) {
            isSameOrigin = false;
        }
        if (!isSameOrigin)
            return;

        // Always fetch non-GET requests from the network
        if (request.method !== 'GET') {
            event.respondWith(
                fetch(request)
                    .catch(function () {
                        return caches.match(offlineUrl);
                    })
            );
            return;
        }

        event.respondWith(
            fetch(request)
                .then(function (response) {
                    // Stash a copy of this page in the cache
                    addToCache(request, response);
                    return response;
                })
                .catch(function () {
                    return caches.match(request)
                        .then(function (response) {
                            return response || caches.match(offlineUrl);
                        })
                        .catch(function () {
                            var accept = request.headers.get('Accept') || '';
                            if (accept.indexOf('image') !== -1) {
                                return new Response('<svg role="img" aria-labelledby="offline-title" viewBox="0 0 400 300" xmlns="http://www.w3.org/2000/svg"><title id="offline-title">Offline</title><g fill="none" fill-rule="evenodd"><path fill="#D8D8D8" d="M0 0h400v300H0z"/><text fill="#9B9B9B" font-family="Helvetica Neue,Arial,Helvetica,sans-serif" font-size="72" font-weight="bold"><tspan x="93" y="172">offline</tspan></text></g></svg>', { headers: { 'Content-Type': 'image/svg+xml' } });
                            }
                        });
                })
        );
    });

})();
