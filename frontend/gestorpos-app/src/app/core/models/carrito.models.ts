import { Producto } from './catalog.models';

export interface ItemCarrito {
  producto: Producto;
  cantidad: number;
}
