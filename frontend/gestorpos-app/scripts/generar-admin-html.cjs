// iOS lee el manifest/título desde el HTML estático que manda el servidor al hacer
// "Agregar a inicio" — no espera a que Angular arranque y lo cambie por JS. Por eso /admin/*
// necesita su propio HTML con el manifest correcto ya puesto, no alcanza con mutar el DOM en runtime.
const fs = require('fs');
const path = require('path');

const DIST_DIR = path.join(__dirname, '..', 'dist', 'gestorpos-app', 'browser');
const ORIGEN = path.join(DIST_DIR, 'index.html');
const DESTINO = path.join(DIST_DIR, 'admin.html');

let html = fs.readFileSync(ORIGEN, 'utf8');
html = html.replace('href="manifest.webmanifest"', 'href="admin-manifest.webmanifest"');
html = html.replace('name="apple-mobile-web-app-title" content="GestorPOS"', 'name="apple-mobile-web-app-title" content="GestorPOS Admin"');
// El ícono de "Agregar a inicio" en iPhone sale de este tag, no del manifest: el de admin va en grafito para no confundirlo con el de los clientes.
html = html.replace('rel="apple-touch-icon" href="icons/icon-192x192.png"', 'rel="apple-touch-icon" href="icons/admin/icon-192x192.png"');

fs.writeFileSync(DESTINO, html);
console.log('admin.html generado en', DESTINO);
