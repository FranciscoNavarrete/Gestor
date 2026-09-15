export interface RankingProductoDto {
  productoId: string;
  productoNombre: string;
  cantidadVendida: number;
  totalVendido: number;
}

export interface GananciasDto {
  cantidadVentas: number;
  totalVentas: number;
  totalCosto: number;
  gananciaNeta: number;
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
}

export interface AbrirCajaRequest {
  montoApertura: number;
}

export interface CerrarCajaRequest {
  montoCierreReal: number;
}
