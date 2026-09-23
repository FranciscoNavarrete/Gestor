export interface CrearFacturaProveedorRequest {
  proveedorId: string;
  numeroFactura: string;
  monto: number;
  fechaEmision: string;
  fechaVencimiento: string | null;
}

export interface RegistrarPagoFacturaRequest {
  monto: number;
  medioPago: string;
}

export type EstadoFactura = 'Pendiente' | 'Pago parcial' | 'Pagada';

export interface FacturaProveedorDto {
  id: string;
  proveedorId: string;
  proveedorNombre: string;
  numeroFactura: string;
  monto: number;
  saldo: number;
  estado: EstadoFactura;
  fechaEmision: string;
  fechaVencimiento: string | null;
  fechaCreacion: string;
}

export interface ResumenFacturasProveedorDto {
  totalAPagar: number;
  cantidadPendientes: number;
}
