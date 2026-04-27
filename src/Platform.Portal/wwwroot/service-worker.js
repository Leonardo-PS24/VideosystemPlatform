const CACHE_NAME = 'videosystem-platform-cache-v2'; // Versione incrementata per forzare l'aggiornamento
const urlsToCache = [
    '/',
    '/css/site.css',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/jquery/dist/jquery.min.js',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/6.0.1/signalr.min.js',
    'https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500;700&display=swap',
    'https://fonts.googleapis.com/icon?family=Material+Icons',
    '/manifest.json',
    '/images/icons/icon-192x192.png',
    '/images/icons/icon-512x512.png'
];

// Evento 'install': Mette in cache le risorse principali.
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('Cache aperta. Aggiungo le risorse dell\'App Shell.');
                // Usiamo cache.add per ogni risorsa per evitare che un singolo fallimento blocchi tutto.
                const cachePromises = urlsToCache.map(urlToCache => {
                    return cache.add(urlToCache).catch(reason => {
                        console.warn(`Impossibile mettere in cache ${urlToCache}: ${reason}`);
                    });
                });
                return Promise.all(cachePromises);
            })
    );
});

// Evento 'fetch': Intercetta le richieste e usa una strategia "Network falling back to cache".
self.addEventListener('fetch', event => {
    // Ignora le richieste che non sono GET
    if (event.request.method !== 'GET') {
        return;
    }

    event.respondWith(
        // 1. Prova prima la rete
        fetch(event.request)
            .then(networkResponse => {
                // Se la richiesta di rete ha successo, aggiorna la cache
                const responseToCache = networkResponse.clone();
                caches.open(CACHE_NAME)
                    .then(cache => {
                        cache.put(event.request, responseToCache);
                    });
                return networkResponse;
            })
            .catch(() => {
                // 2. Se la rete fallisce, prova a servire dalla cache
                return caches.match(event.request);
            })
    );
});

// Evento 'activate': viene eseguito quando il service worker viene attivato.
// Pulisce le vecchie versioni della cache.
self.addEventListener('activate', event => {
    const cacheWhitelist = [CACHE_NAME];
    event.waitUntil(
        caches.keys().then(cacheNames => Promise.all(
            cacheNames.map(cacheName => {
                // Se la cache non è nella whitelist (cioè non è la versione corrente), la cancella.
                if (!cacheWhitelist.includes(cacheName)) {
                    return caches.delete(cacheName);
                }
            })
        ))
    );
});