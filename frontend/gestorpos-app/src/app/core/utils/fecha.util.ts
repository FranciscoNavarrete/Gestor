/** Convierte entre el string "YYYY-MM-DD" que usan los filtros de fecha (y que viaja tal cual a la
 * API) y el Date que necesita el datepicker de Material. Arma la fecha con componentes locales
 * (no ISO parseado directo) para no correr un día por la zona horaria. */
export function isoAFecha(iso: string): Date | null {
  if (!iso) return null;
  const [y, m, d] = iso.split('-').map(Number);
  if (!y || !m || !d) return null;
  return new Date(y, m - 1, d);
}

export function fechaAIso(fecha: Date | null): string {
  if (!fecha) return '';
  const y = fecha.getFullYear();
  const m = String(fecha.getMonth() + 1).padStart(2, '0');
  const d = String(fecha.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}
