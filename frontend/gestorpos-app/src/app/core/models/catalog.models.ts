export interface Categoria {
  id: string;
  nombre: string;
  activo: boolean;
}

export interface CrearCategoriaRequest {
  nombre: string;
}

export interface Producto {
  id: string;
  sku: string;
  nombre: string;
  categoriaId: string | null;
  categoriaNombre: string | null;
  precio: number;
  costo: number;
  stockActual: number;
  stockMinimo: number;
  enStockMinimo: boolean;
  activo: boolean;
}

export interface CrearProductoRequest {
  sku: string;
  nombre: string;
  categoriaId: string | null;
  precio: number;
  costo: number;
  stockActual: number;
  stockMinimo: number;
}

export interface EditarProductoRequest {
  nombre: string;
  categoriaId: string | null;
  precio: number;
  costo: number;
  stockMinimo: number;
}

export interface ActualizarPreciosMasivoRequest {
  porcentaje: number;
  categoriaId: string | null;
}
