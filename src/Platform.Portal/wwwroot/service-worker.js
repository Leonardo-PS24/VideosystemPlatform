// service-worker.js

// Evento di installazione: il service worker viene installato
self.addEventListener('install', event => {
  console.log('Service Worker: installazione in corso...');
  // Forza l'attivazione del nuovo service worker
  self.skipWaiting();
});

// Evento di attivazione: il service worker è pronto a controllare la pagina
self.addEventListener('activate', event => {
  console.log('Service Worker: attivazione completata.');
  // Prende il controllo immediato delle pagine aperte
  return self.clients.claim();
});

// Evento fetch: intercetta le richieste di rete
// Per ora, non facciamo nulla di speciale, lasciamo che la rete gestisca tutto.
// In futuro, qui si può implementare la logica di caching.
self.addEventListener('fetch', event => {
  // Non fare nulla, usa la strategia network-first di default
});
