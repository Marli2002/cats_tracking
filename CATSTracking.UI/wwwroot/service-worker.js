/* global self, caches, fetch */

const PRECACHE = 'tracker-precache-v1';
const RUNTIME = 'tracker-runtime-v1';

// Core assets to cache immediately
const PRECACHE_URLS = [
    '/',
    '/offline.html',
    '/css/site.css',
    '/js/site.js',
    '/icons/tracker-icon.png',
    '/icons/ts-icon.png'
];

// Install event: cache core assets
self.addEventListener('install', (event) => {
    self.skipWaiting();
    event.waitUntil(
        caches.open(PRECACHE).then((cache) => cache.addAll(PRECACHE_URLS))
    );
});

// Activate event: cleanup old caches and take control
self.addEventListener('activate', (event) => {
    event.waitUntil(
        Promise.all([
            caches.keys().then((cacheNames) =>
                Promise.all(
                    cacheNames
                        .filter((name) => name !== PRECACHE && name !== RUNTIME)
                        .map((name) => caches.delete(name))
                )
            ),
            self.clients.claim()
        ])
    );
});

// Fetch event: serve cached content or fallback
self.addEventListener('fetch', (event) => {
    const { request } = event;

    // Only handle GET requests
    if (request.method !== 'GET') return;

    // Navigation requests (HTML pages)
    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request)
                .then((response) => {
                    // Cache a copy for runtime
                    const copy = response.clone();
                    caches.open(RUNTIME).then((cache) => cache.put(request, copy));
                    return response;
                })
                .catch(async () => {
                    // Offline fallback
                    return (await caches.match(request)) || caches.match('/offline.html');
                })
        );
        return;
    }

    // Static assets: cache-first strategy
    const url = new URL(request.url);
    if (url.origin === self.location.origin) {
        event.respondWith(
            caches.match(request).then((cached) => {
                if (cached) return cached;

                return fetch(request).then((response) => {
                    const copy = response.clone();
                    caches.open(RUNTIME).then((cache) => cache.put(request, copy));
                    return response;
                });
            })
        );
    }
});
