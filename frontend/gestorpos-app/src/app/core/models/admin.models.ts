import { FEATURE_COMPRAS } from './compras-feature';
import { FEATURE_CUENTA_CORRIENTE } from './cuenta-corriente';
import { FEATURE_FACTURAS_PROVEEDOR } from './facturas-proveedor-feature';
import { FEATURE_MENU_LATERAL } from './menu-lateral';
import { FEATURE_TAREAS } from './tareas-feature';

export interface CrearNegocioRequest {
  nombreNegocio: string;
  nombreAdmin: string;
  email: string;
  password: string;
  vendedorId?: string | null;
  mpPlanId?: number | null;
  cardToken?: string | null;
  /** Primer pago recibido por fuera de Mercado Pago: 'Efectivo' (lo recibe quien da el alta) o 'Transferencia'. */
  primerPago?: 'Efectivo' | 'Transferencia' | null;
  primerPagoMonto?: number | null;
  primerPagoNota?: string | null;
}

export interface ConfirmarPagoRequest {
  metodo: 'Transferencia' | 'Tarjeta' | 'Otro' | 'Efectivo';
  fechaRecepcion?: string | null;
  nota?: string | null;
  monto?: number | null;
}

export interface FluxoPlan {
  mpPlanId: number;
  nombre: string;
  monto: number;
  moneda: string;
  tipoFrecuencia: string;
  frecuencia: number;
  diasGratis: number;
  montoPrimerCobro?: number | null;
}

export interface TenantResumen {
  id: string;
  nombre: string;
  slug: string;
  activo: boolean;
  fechaCreacion: string;
  vendedorId: string | null;
  vendedorNombre: string | null;
  fluxoClienteId?: number | null;
  fluxoSuscripcionId?: number | null;
  fluxoInitPoint?: string | null;
  fluxoEstado?: string | null;
  fluxoPrimerCobroAprobado?: boolean;
  fluxoAjustePendiente?: boolean;
  fluxoCobroRechazado?: boolean;
  fluxoMotivoRechazo?: string | null;
  fluxoAccesoHasta?: string | null;
  fluxoProximoCobro?: string | null;
  formaPrimerPago?: 'Efectivo' | 'Transferencia' | 'Tarjeta' | 'Otro' | null;
  pagoManualEstado?: 'Pendiente' | 'Confirmado' | null;
  pagoManualMonto?: number | null;
  pendienteActivacion?: boolean;
}

export interface CobroNegocio {
  fecha: string | null;
  monto: number;
  estado: 'aprobado' | 'rechazado' | 'pendiente' | 'cancelado';
  motivo: string | null;
  intento: number;
  proximoReintento: string | null;
  esPrimerCobro: boolean;
}

export interface CobrosNegocio {
  estado: string;
  montoMensual: number;
  proximoCobro: string | null;
  proximoMonto: number | null;
  cobros: CobroNegocio[];
}

export interface LiquidacionItem {
  tenantId: string;
  nombre: string;
  fechaAltaUtc: string;
  orden: number;
  comision: number;
  bono: number;
}

export interface Liquidacion {
  id: string;
  vendedorId: string;
  vendedorNombre: string;
  anio: number;
  mes: number;
  estado: 'Pendiente' | 'Pagada';
  fechaCierreUtc: string;
  fechaPago: string | null;
  nota: string | null;
  ventas: number;
  totalComision: number;
  totalBono: number;
  total: number;
  items: LiquidacionItem[];
}

export interface LiquidacionVendedor {
  vendedorId: string;
  nombre: string;
  ventas: number;
  comision: number;
  bono: number;
  ventasSinLiquidar: number;
  comisionSinLiquidar: number;
  bonoSinLiquidar: number;
  esperandoCobro: number;
  liquidaciones: Liquidacion[];
}

export interface LiquidacionesMes {
  anio: number;
  mes: number;
  vendedores: LiquidacionVendedor[];
}

export interface PrevisualizacionLiquidacion {
  vendedorId: string;
  vendedorNombre: string;
  anio: number;
  mes: number;
  items: LiquidacionItem[];
  totalComision: number;
  totalBono: number;
  total: number;
  esperandoCobro: number;
}

export interface ClienteEnRiesgo {
  tenantId: string;
  nombre: string;
  tipo: 'primer-cobro' | 'rechazado' | 'pausado' | 'suspendido';
  motivo: string | null;
  monto: number;
  proximoReintento: string | null;
}

export interface AltasMes {
  anio: number;
  mes: number;
  altas: number;
}

export interface ResumenFinanciero {
  ingresoMensual: number;
  clientesActivos: number;
  esperandoPrimerCobro: number;
  porCobrarPrimerosCobros: number;
  altasMes: number;
  bajasMes: number;
  porcentajeBajas: number;
  aCobrar30Dias: number;
  cobrosRechazados: number;
  montoRechazado: number;
  pausadosOSuspendidos: number;
  enRiesgo: ClienteEnRiesgo[];
  altasPorMes: AltasMes[];
}

export interface MovimientoAdmin {
  id: string;
  fechaUtc: string;
  adminId: string;
  adminNombre: string;
  adminRol: string;
  accion: string;
  entidad: string;
  entidadId: string | null;
  entidadNombre: string;
  detalle: string | null;
}

export interface MovimientosAdmin {
  desde: string;
  hasta: string;
  total: number;
  items: MovimientoAdmin[];
}

export interface LinkPago {
  estado: string;
  link: string | null;
}

export type AdminRol = 'Operador' | 'Vendedor';

export interface AdminUsuario {
  id: string;
  email: string;
  nombre: string;
  rol: AdminRol;
  activo: boolean;
  creadoUtc: string;
}

export interface CrearAdminUsuarioRequest {
  email: string;
  password: string;
  nombre: string;
  rol: AdminRol;
}

export interface TenantFeature {
  id: string;
  clave: string;
  habilitado: boolean;
}

export interface FeatureCatalogoItem {
  clave: string;
  nombre: string;
  descripcion: string;
}

/** Catálogo de funcionalidades que se pueden activar por negocio. Se suma una entrada acá cada vez
 * que se construye una funcionalidad nueva gateada por feature flag. */
export const CATALOGO_FEATURES: FeatureCatalogoItem[] = [
  {
    clave: FEATURE_CUENTA_CORRIENTE,
    nombre: 'Cuenta corriente (A cuenta)',
    descripcion: 'Vender fiado y llevar la deuda de cada cliente',
  },
  {
    clave: FEATURE_MENU_LATERAL,
    nombre: 'Menú lateral (desktop)',
    descripcion: 'En pantallas grandes, navegación fija al costado en vez de la barra de abajo',
  },
  {
    clave: FEATURE_COMPRAS,
    nombre: 'Proveedores y compras',
    descripcion: 'Registrar compras a proveedores — suma stock y actualiza costos automáticamente',
  },
  {
    clave: FEATURE_FACTURAS_PROVEEDOR,
    nombre: 'Facturas a proveedores',
    descripcion: 'Cargar facturas recibidas y llevar el control de qué le debés a cada proveedor',
  },
  {
    clave: FEATURE_TAREAS,
    nombre: 'Tareas',
    descripcion: 'Agendar tareas con fecha y hora, con recordatorio por notificación push',
  },
];

export interface ReporteResumen {
  ventas: number;
  pendientes: number;
  comision: number;
  ventasPrimeras: number;
  tarifaPrimeras: number;
  ventasSiguientes: number;
  tarifaSiguientes: number;
  bono: number;
  bonoVentas: number;
  bonoMonto: number;
}

export interface ReporteVendedor {
  vendedorId: string;
  nombre: string;
  ventas: number;
  pendientes: number;
  comision: number;
  bono: number;
  ventasMes: number;
}

export type EstadoVenta = 'suscripto' | 'baja' | 'esperando' | 'pendiente' | 'cancelada';

export interface ReporteVentaItem {
  tenantId: string;
  nombre: string;
  fechaAlta: string;
  vendedorId: string;
  vendedorNombre: string;
  estado: EstadoVenta;
  comision: number | null;
  bono: number | null;
}

export interface ReporteVentas {
  desde: string;
  hasta: string;
  resumen: ReporteResumen;
  vendedores: ReporteVendedor[];
  items: ReporteVentaItem[];
}
