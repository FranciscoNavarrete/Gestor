/** WhatsApp de soporte de GestorPOS (formato internacional, sin + ni espacios). */
export const SOPORTE_WHATSAPP = '5491125113895';

export function urlSoporte(mensaje: string): string {
  return `https://wa.me/${SOPORTE_WHATSAPP}?text=${encodeURIComponent(mensaje)}`;
}

export function abrirSoporte(mensaje: string): void {
  window.open(urlSoporte(mensaje), '_blank', 'noopener');
}
