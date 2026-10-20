/** Cuentas de la ayuda para poner precios. La ganancia es en % sobre el costo: (precio - costo) / costo. */

export function redondearImporte(valor: number): number {
  return Math.round((valor + Number.EPSILON) * 100) / 100;
}

export function precioDesdeGanancia(costo: number, gananciaPorcentaje: number): number {
  return redondearImporte(costo * (1 + gananciaPorcentaje / 100));
}

/** null si el costo es cero o no está cargado (no hay sobre qué calcular el %). */
export function gananciaDesdePrecio(costo: number, precio: number): number | null {
  if (!costo || costo <= 0) return null;
  return Math.round(((precio - costo) / costo) * 1000) / 10;
}

/** Redondeo cómodo para cobrar: de a $1 hasta $99, de a $10 hasta $999 y de a $50 desde $1000. */
export function redondearParaCobrar(precio: number): number {
  const paso = precio < 100 ? 1 : precio < 1000 ? 10 : 50;
  return Math.round(precio / paso) * paso;
}
