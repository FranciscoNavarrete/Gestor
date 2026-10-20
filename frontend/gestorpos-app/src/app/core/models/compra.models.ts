export interface Proveedor {
  id: string;
  nombre: string;
  cuit: string | null;
  telefono: string | null;
  email: string | null;
  activo: boolean;
}

export interface CrearProveedorRequest {
  nombre: string;
  cuit: string | null;
  telefono: string | null;
  email: string | null;
}

export interface EditarProveedorRequest {
  nombre: string;
  cuit: string | null;
  telefono: string | null;
  email: string | null;
}

export interface ItemCompraRequest {
  productoId: string;
  cantidad: number;
  costoUnitario: number;
  /** Con presentación, cantidad cuenta presentaciones y costoUnitario es lo que costó cada una. */
  presentacionId?: string | null;
}

export interface CrearCompraRequest {
  proveedorId: string;
  items: ItemCompraRequest[];
}

export interface CompraItemDto {
  productoId: string;
  productoNombre: string;
  cantidad: number;
  costoUnitario: number;
  subtotal: number;
  presentacionNombre?: string | null;
  unidad?: string;
}

export interface CompraDto {
  id: string;
  fecha: string;
  proveedorId: string;
  proveedorNombre: string;
  total: number;
  items: CompraItemDto[];
}

export interface CompraResumenDto {
  id: string;
  fecha: string;
  proveedorNombre: string;
  cantidadItems: number;
  total: number;
}

export interface ResumenComprasDto {
  cantidadCompras: number;
  totalComprado: number;
  cantidadProveedores: number;
}
