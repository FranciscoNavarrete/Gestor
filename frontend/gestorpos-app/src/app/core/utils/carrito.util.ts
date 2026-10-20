import { ItemCarrito } from '../models/carrito.models';
import { Producto } from '../models/catalog.models';
import { esMedida, formatearCantidad, formatearNumero } from './cantidad.util';

function redondear(valor: number, decimales: number): number {
  const factor = 10 ** decimales;
  return Math.round((valor + Number.EPSILON) * factor) / factor;
}

/** Precio de cada unidad de la línea: de la presentación si la hay, o el del producto (por kilo, litro, unidad...). */
export function precioDeLinea(item: ItemCarrito): number {
  return item.presentacion ? item.presentacion.precio : item.producto.precio;
}

export function subtotalDeLinea(item: ItemCarrito): number {
  return redondear(precioDeLinea(item) * item.cantidad, 2);
}

export function totalDeCarrito(items: ItemCarrito[]): number {
  return redondear(items.reduce((acc, i) => acc + subtotalDeLinea(i), 0), 2);
}

/** Lo que la línea descuenta del stock del producto, en su unidad (kilos, litros, metros o unidades). */
export function cantidadBaseDeLinea(item: ItemCarrito): number {
  return redondear(item.cantidad * (item.presentacion?.cantidadBase ?? 1), 3);
}

/** Stock que ya tienen comprometido las líneas del carrito para un producto, sin contar una línea (la que se edita). */
export function baseEnCarrito(items: ItemCarrito[], productoId: string, excepto?: ItemCarrito | null): number {
  return redondear(
    items
      .filter((i) => i.producto.id === productoId && i !== excepto)
      .reduce((acc, i) => acc + cantidadBaseDeLinea(i), 0),
    3,
  );
}

export function mismaLinea(a: ItemCarrito, b: ItemCarrito): boolean {
  return a.producto.id === b.producto.id && (a.presentacion?.id ?? null) === (b.presentacion?.id ?? null);
}

/** Línea de un producto común: por unidad y sin presentación. Se maneja con los botones de más y menos de siempre. */
export function esLineaSimple(item: ItemCarrito): boolean {
  return !item.presentacion && !esMedida(item.producto.unidad);
}

/** Línea suelta de un producto medido: la cantidad se edita con el selector, no con más y menos. */
export function esLineaMedida(item: ItemCarrito): boolean {
  return !item.presentacion && esMedida(item.producto.unidad);
}

/** "0,35 kg", "2 × Bolsa 15 kg" o "3" para el detalle de una línea. */
export function textoCantidadDeLinea(item: ItemCarrito): string {
  if (item.presentacion) return `${formatearNumero(item.cantidad)} × ${item.presentacion.nombre}`;
  return esMedida(item.producto.unidad) ? formatearCantidad(item.cantidad, item.producto.unidad) : formatearNumero(item.cantidad);
}

/** ¿Hace falta el selector de cantidad? Sí si el producto es medido o tiene presentaciones; si no, se suma de a uno. */
export function necesitaSelector(producto: Producto): boolean {
  return esMedida(producto.unidad) || producto.presentaciones.length > 0;
}

/** Cuántas "cosas" se llevan, para el resumen del carrito: los productos sueltos medidos cuentan de a uno. */
export function cantidadParaResumen(items: ItemCarrito[]): number {
  return items.reduce((acc, i) => acc + (esLineaMedida(i) ? 1 : i.cantidad), 0);
}
