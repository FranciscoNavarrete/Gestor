export interface Categoria {
  id: string;
  nombre: string;
  activo: boolean;
}

export interface Marca {
  id: string;
  nombre: string;
  activo: boolean;
}

export interface CrearMarcaRequest {
  nombre: string;
}

export interface EditarMarcaRequest {
  nombre: string;
}

export interface CrearCategoriaRequest {
  nombre: string;
}

export interface EditarCategoriaRequest {
  nombre: string;
}

export type UnidadMedida = 'Unidad' | 'Kilo' | 'Litro' | 'Metro';

/** Forma alternativa de vender un producto ("Bolsa 15 kg"). cantidadBase está en la unidad del producto. */
export interface Presentacion {
  id: string;
  productoId: string;
  nombre: string;
  cantidadBase: number;
  precio: number;
  activo: boolean;
}

export interface CrearPresentacionRequest {
  nombre: string;
  cantidadBase: number;
  precio: number;
}

export type EditarPresentacionRequest = CrearPresentacionRequest;

export interface Producto {
  id: string;
  sku: string;
  nombre: string;
  categoriaId: string | null;
  categoriaNombre: string | null;
  marcaId: string | null;
  marcaNombre: string | null;
  precio: number;
  costo: number;
  stockActual: number;
  stockMinimo: number;
  enStockMinimo: boolean;
  activo: boolean;
  unidad: UnidadMedida;
  presentaciones: Presentacion[];
}

export interface CrearProductoRequest {
  sku: string;
  nombre: string;
  categoriaId: string | null;
  precio: number;
  costo: number;
  stockActual: number;
  stockMinimo: number;
  /** Solo con la función "Medidas y presentaciones". Sin dato, el producto es por unidad. */
  unidad?: UnidadMedida;
  presentaciones?: CrearPresentacionRequest[];
  marcaId?: string | null;
}

export interface EditarProductoRequest {
  nombre: string;
  categoriaId: string | null;
  marcaId?: string | null;
  precio: number;
  costo: number;
  stockMinimo: number;
}

export interface ActualizarPreciosMasivoRequest {
  porcentaje: number;
  categoriaId: string | null;
  marcaId: string | null;
}

export interface AlcancePreciosMasivo {
  productos: number;
  presentaciones: number;
}

export interface ResultadoPreciosMasivo {
  productosActualizados: number;
  presentacionesActualizadas: number;
}

export interface ImportarProductoError {
  fila: number;
  mensaje: string;
}

export interface ImportarProductosResultado {
  totalFilas: number;
  creados: number;
  actualizados: number;
  errores: ImportarProductoError[];
}

export interface PaginaProductos {
  items: Producto[];
  pagina: number;
  tamanoPagina: number;
  totalItems: number;
  totalPaginas: number;
}
