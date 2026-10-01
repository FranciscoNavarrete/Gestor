// iOS (Safari 16.4+) lee start_url/scope del manifest al hacer "Agregar a inicio", así que un
// único manifest con scope "./" siempre te manda al login del negocio sin importar desde qué URL
// instalaste el ícono. Mientras se está en /admin, se apunta a un manifest propio con su scope.
const SELECTOR_MANIFEST = 'link[rel="manifest"]';
const SELECTOR_TITULO_IOS = 'meta[name="apple-mobile-web-app-title"]';

export function activarManifestAdmin(): void {
  document.querySelector(SELECTOR_MANIFEST)?.setAttribute('href', 'admin-manifest.webmanifest');
  document.querySelector(SELECTOR_TITULO_IOS)?.setAttribute('content', 'GestorPOS Admin');
}

export function restaurarManifestNegocio(): void {
  document.querySelector(SELECTOR_MANIFEST)?.setAttribute('href', 'manifest.webmanifest');
  document.querySelector(SELECTOR_TITULO_IOS)?.setAttribute('content', 'GestorPOS');
}
