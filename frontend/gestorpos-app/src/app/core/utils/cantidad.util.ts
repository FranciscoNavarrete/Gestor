import { UnidadMedida } from '../models/catalog.models';

export interface OpcionUnidad {
  valor: UnidadMedida;
  etiqueta: string;
  simbolo: string;
}

export const UNIDADES: readonly OpcionUnidad[] = [
  { valor: 'Unidad', etiqueta: 'Unidad', simbolo: 'u' },
  { valor: 'Kilo', etiqueta: 'Kilo', simbolo: 'kg' },
  { valor: 'Litro', etiqueta: 'Litro', simbolo: 'L' },
  { valor: 'Metro', etiqueta: 'Metro', simbolo: 'm' },
];

export function esMedida(unidad: UnidadMedida | undefined): boolean {
  return !!unidad && unidad !== 'Unidad';
}

export function simboloUnidad(unidad: UnidadMedida | undefined): string {
  return UNIDADES.find((u) => u.valor === unidad)?.simbolo ?? 'u';
}

/** Número para mostrar: coma decimal, hasta 3 decimales, sin ceros de más y sin separador de miles. */
export function formatearNumero(valor: number): string {
  return new Intl.NumberFormat('es-AR', { maximumFractionDigits: 3, useGrouping: false }).format(valor);
}

/** "12 u." para lo de siempre; "35,5 kg", "3 L" o "2,4 m" para lo medido. */
export function formatearCantidad(valor: number, unidad: UnidadMedida | undefined = 'Unidad'): string {
  return unidad === 'Unidad' || !unidad ? `${formatearNumero(valor)} u.` : `${formatearNumero(valor)} ${simboloUnidad(unidad)}`;
}

/** Importe en pesos: entero tal cual ($4200), con decimales en formato argentino ($1305,50). */
export function formatearPesos(valor: number): string {
  return Number.isInteger(valor) ? `$${valor}` : `$${valor.toFixed(2).replace('.', ',')}`;
}

/** Menos de 3 decimales: 1000 g = 1 kg, 1 ml = 0,001 L. */
export function tieneMasDeTresDecimales(valor: number): boolean {
  return Math.abs(Math.round(valor * 1000) - valor * 1000) > 1e-6;
}
