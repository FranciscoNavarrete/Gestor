export interface Cliente {
  id: string;
  telefono: string;
  nombre: string | null;
  fechaAlta: string;
  cantidadCompras: number;
}

export interface ClienteDetalle extends Cliente {
  totalGastado: number;
  ultimaCompra: string | null;
}

export interface ClienteVenta {
  id: string;
  fecha: string;
  total: number;
  medioPago: string;
}

export interface EditarClienteRequest {
  nombre: string | null;
  telefono: string;
}

export interface PaginaClientes {
  items: Cliente[];
  pagina: number;
  tamanoPagina: number;
  totalItems: number;
  totalPaginas: number;
}
