// Service worker de Ecos del Grimorio (versión web).
// Guarda el juego en el celular la primera vez que se abre, así la siguiente
// vez carga rápido y se puede jugar aunque no haya internet.
// La versión cambia cada vez que se compila para que se descargue la nueva.
const version = "ecos-del-grimorio-20261026-1746";

const archivos = [
  "./",
  "index.html",
  "manifest.webmanifest",
  "TemplateData/estilo.css",
  "TemplateData/Poppins-Bold.ttf",
  "TemplateData/lira.png",
  "TemplateData/favicon.png",
  "TemplateData/icono-180.png",
  "TemplateData/icono-192.png",
  "TemplateData/icono-512.png",
  "Build/BuildWeb.loader.js",
  "Build/BuildWeb.framework.js.unityweb",
  "Build/BuildWeb.data.unityweb",
  "Build/BuildWeb.wasm.unityweb",
];

self.addEventListener("install", function (e) {
  e.waitUntil(
    caches.open(version).then(function (cache) { return cache.addAll(archivos); })
      .then(function () { return self.skipWaiting(); })
  );
});

// Borra lo guardado de versiones anteriores
self.addEventListener("activate", function (e) {
  e.waitUntil(
    caches.keys().then(function (nombres) {
      return Promise.all(nombres.filter(function (n) { return n !== version; })
                                .map(function (n) { return caches.delete(n); }));
    }).then(function () { return self.clients.claim(); })
  );
});

// La página se pide primero a internet (para ver si hay versión nueva);
// todo lo demás se toma de lo guardado.
self.addEventListener("fetch", function (e) {
  if (e.request.method !== "GET") return;
  var esPagina = e.request.mode === "navigate";
  e.respondWith(
    esPagina
      ? fetch(e.request).then(function (r) {
          var copia = r.clone();
          caches.open(version).then(function (c) { c.put(e.request, copia); });
          return r;
        }).catch(function () { return caches.match(e.request).then(function (r) { return r || caches.match("index.html"); }); })
      : caches.match(e.request).then(function (r) {
          return r || fetch(e.request).then(function (resp) {
            if (resp.ok) {
              var copia = resp.clone();
              caches.open(version).then(function (c) { c.put(e.request, copia); });
            }
            return resp;
          });
        })
  );
});
