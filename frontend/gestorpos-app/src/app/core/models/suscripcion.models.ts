import { PromoSuscripcion } from './admin.models';

export interface CobroSuscripcion {
  fecha: string | null;
  monto: number;
  estado: 'aprobado' | 'rechazado' | 'pendiente' | 'cancelado';
  motivo: string | null;
  intento: number;
  proximoReintento: string | null;
  esPrimerCobro: boolean;
}

export interface MiSuscripcion {
  estado: string;
  montoMensual: number;
  proximoCobro: string | null;
  proximoMonto: number | null;
  tarjetaEditable: boolean;
  cobroRechazado: boolean;
  motivoRechazo: string | null;
  proximoReintento: string | null;
  emailPagador: string;
  cobros: CobroSuscripcion[];
  primerPagoManual?: boolean;
  planNombre?: string | null;
  montoNormal?: number;
  promo?: PromoSuscripcion | null;
}
