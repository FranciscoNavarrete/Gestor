import { Presentacion, Producto } from './catalog.models';

/** Una línea del carrito. Sin presentación es el producto suelto (por unidad, o por kilo, litro o metro);
 * con presentación, cantidad cuenta presentaciones ("2 × Bolsa 15 kg"). */
export interface ItemCarrito {
  producto: Producto;
  cantidad: number;
  presentacion?: Presentacion | null;
}
