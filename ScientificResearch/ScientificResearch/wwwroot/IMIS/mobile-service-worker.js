const CACHE_NAME = 'scientificresearch-mobile-shell-v1';
const SHELL = [
  '/IMIS/views/科研办公/科研办公/移动科研工作台.html',
  '/IMIS/mobile-manifest.webmanifest',
  '/IMIS/css/child_pages.css',
  '/IMIS/libs/bootstrap/css/bootstrap.min.css',
  '/IMIS/libs/bootstrap/js/bootstrap.min.js',
  '/IMIS/libs/jquery-1.11.1.min.js',
  '/IMIS/js/get_connect/connect_http.js'
];
self.addEventListener('install', event => {
  event.waitUntil(caches.open(CACHE_NAME).then(cache => cache.addAll(SHELL)).then(() => self.skipWaiting()));
});
self.addEventListener('activate', event => {
  event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key)))).then(() => self.clients.claim()));
});
self.addEventListener('fetch', event => {
  const request = event.request;
  if (request.method !== 'GET' || new URL(request.url).origin !== self.location.origin) return;
  event.respondWith(fetch(request).then(response => {
    if (response.ok && request.url.includes('/IMIS/')) {
      const copy = response.clone(); caches.open(CACHE_NAME).then(cache => cache.put(request, copy));
    }
    return response;
  }).catch(() => caches.match(request).then(cached => cached || caches.match('/IMIS/views/科研办公/科研办公/移动科研工作台.html'))));
});
