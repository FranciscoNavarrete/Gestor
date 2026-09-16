export interface MedioPagoDto {
  id: string;
  nombre: string;
  activo: boolean;
  esProtegido: boolean;
}

export interface CrearMedioPagoRequest {
  nombre: string;
}

export interface EditarMedioPagoRequest {
  nombre: string;
}

export interface NegocioDto {
  nombre: string;
  telefono: string | null;
  tieneLogo: boolean;
}

export interface ActualizarNegocioRequest {
  nombre: string;
  telefono: string | null;
}
