const CACHE_NAME = 'aim-classic-mobile-v62';
const ASSETS = [
  './',
  './index.html',
  './styles.css',
  './app.js',
  './manifest.webmanifest',
  './icons/icon-192.png',
  './icons/icon-512.png',
  './icons/apple-touch-icon.png',
  './icons/aero_bg.jpg',
  './banners/banner_aero.jpg',
  './banners/banner_globe.jpg',
  './banners/banner_mail.jpg',
  './banners/banner_music.jpg',
  './banners/banner_workshop.jpg',
  './avatars/aim_icon.gif',
  './sounds/welcome.wav',
  './sounds/youve_got_mail.wav',
  './sounds/im_send.wav',
  './sounds/im_receive.wav',
  './sounds/door_open.wav',
  './sounds/ring.wav',
  './sounds/buddy_ding.wav'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => cache.addAll(ASSETS)).catch(() => {})
  );
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k)))
    )
  );
  self.clients.claim();
});

self.addEventListener('fetch', (event) => {
  const url = event.request.url;
  if (url.includes('ntfy.sh') || url.includes('/api/') || event.request.method !== 'GET') {
    return;
  }
  event.respondWith(
    caches.match(event.request).then((cached) => cached || fetch(event.request))
  );
});
