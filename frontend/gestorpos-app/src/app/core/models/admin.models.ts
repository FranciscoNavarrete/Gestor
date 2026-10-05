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
