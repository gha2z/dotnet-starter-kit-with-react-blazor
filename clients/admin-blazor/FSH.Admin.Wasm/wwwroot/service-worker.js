// FSH Blazor WASM Service Worker — v2.0
// Cache strategy: network-first with cache fallback for everything. _framework
// URLs are NOT content-hashed (boot.json + dll names are stable across builds),
// so a cache-first strategy pins stale builds until the cache is purged.
const CACHE_NAME = 'fsh-pwa-v2.0';
const OFFLINE_URL = 'offline.html';
const PRECACHE_URLS = [
  './',
  './manifest.json',
  './offline.html'
];

self.addEventListener('install', (e) => {
  e.waitUntil(
    caches.open(CACHE_NAME).then((cache) => cache.addAll(PRECACHE_URLS))
  );
  self.skipWaiting();
});

self.addEventListener('activate', (e) => {
  e.waitUntil(
    caches.keys().then((names) =>
      Promise.all(names.filter((n) => n !== CACHE_NAME).map((n) => caches.delete(n)))
    )
  );
  self.clients.claim();
});

self.addEventListener('fetch', (e) => {
  const url = new URL(e.request.url);

  // Only handle same-origin requests
  if (url.origin !== location.origin) return;

  // Navigation requests: network-first, offline fallback
  if (e.request.mode === 'navigate') {
    e.respondWith(
      fetch(e.request).catch(() =>
        caches.match(OFFLINE_URL).then((r) => r || new Response('Offline', { status: 503 }))
      )
    );
    return;
  }

  // _framework assets: network-first with cache fallback (see header comment
  // — cache-first pins stale builds because these URLs are not content-hashed).
  // Everything else: network-first with cache fallback
  e.respondWith(
    fetch(e.request)
      .then((response) => {
        const clone = response.clone();
        caches.open(CACHE_NAME).then((cache) => cache.put(e.request, clone));
        return response;
      })
      .catch(() => caches.match(e.request))
  );
});
