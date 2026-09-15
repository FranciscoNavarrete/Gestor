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
