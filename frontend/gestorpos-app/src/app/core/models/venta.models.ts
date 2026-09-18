export type MedioPago = string;

export interface ItemVentaRequest {
  productoId: string;
  cantidad: number;
}

export interface CrearVentaRequest {
  items: ItemVentaRequest[];
  medioPago: MedioPago;
  telefonoCliente: string | null;
  nombreCliente: string | null;
}

export interface VentaItemDto {
  productoId: string;
  productoNombre: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
}

export interface VentaDto {
  id: string;
  fecha: string;
  total: number;
  medioPago: string;
  telefonoCliente: string | null;
  items: VentaItemDto[];
  ticketTexto: string;
  whatsAppLink: string | null;
}

export interface VentaResumenDto {
  id: string;
  fecha: string;
  total: number;
  medioPago: string;
}
