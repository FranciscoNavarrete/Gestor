export interface RankingProductoDto {
  productoId: string;
  productoNombre: string;
  cantidadVendida: number;
  totalVendido: number;
}

export interface RankingClienteDto {
  clienteId: string;
  nombre: string | null;
  telefono: string;
  cantidadCompras: number;
  totalGastado: number;
}

export interface GananciasDto {
  cantidadVentas: number;
  totalVentas: number;
  totalCosto: number;
  gananciaNeta: number;
}

export interface DeudaClienteDto {
  clienteId: string;
  nombre: string | null;
  telefono: string;
  cantidadVentasACuenta: number;
  saldo: number;
}

export interface DeudasDto {
  totalAdeudado: number;
  cantidadClientes: number;
  clientes: DeudaClienteDto[];
}

export interface DashboardDto {
  ventasHoy: number;
  cantidadVentasHoy: number;
  gananciaHoy: number;
  ventasMes: number;
  productosBajoStockMinimo: number;
  productoMasVendidoHoy: RankingProductoDto | null;
}

export interface VentaPorMedioPagoDto {
  medioPago: string;
  total: number;
}

export interface MovimientoCajaDto {
  id: string;
  tipo: 'Ingreso' | 'Egreso';
  monto: number;
  motivo: string;
  fecha: string;
}

export interface CajaDto {
  id: string;
  fechaApertura: string;
  montoApertura: number;
  abierta: boolean;
  montoCierreEsperado: number | null;
  montoCierreReal: number | null;
  diferencia: number | null;
  fechaCierre: string | null;
  ventasPorMedioPago: VentaPorMedioPagoDto[];
  movimientos: MovimientoCajaDto[];
  netoMovimientos: number;
}

export interface AbrirCajaRequest {
  montoApertura: number;
}

export interface CerrarCajaRequest {
  montoCierreReal: number;
}

export interface CrearMovimientoCajaRequest {
  tipo: 'Ingreso' | 'Egreso';
  monto: number;
  motivo: string;
}
