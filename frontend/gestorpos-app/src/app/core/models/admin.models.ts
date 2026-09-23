import { FEATURE_COMPRAS } from './compras-feature';
import { FEATURE_CUENTA_CORRIENTE } from './cuenta-corriente';
import { FEATURE_MENU_LATERAL } from './menu-lateral';

export interface CrearNegocioRequest {
  nombreNegocio: string;
  nombreAdmin: string;
  email: string;
  password: string;
}

export interface TenantResumen {
  id: string;
  nombre: string;
  slug: string;
  activo: boolean;
  fechaCreacion: string;
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
];
