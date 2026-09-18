export interface MovimientoStock {
  id: string;
  productoId: string;
  productoNombre: string;
  cantidad: number;
  stockResultante: number;
  motivo: string;
  usuarioNombre: string;
  fecha: string;
}

export interface PaginaMovimientosStock {
  items: MovimientoStock[];
  pagina: number;
  tamanoPagina: number;
  totalItems: number;
  totalPaginas: number;
}
