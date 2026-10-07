import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PromoSuscripcion } from '../../../core/models/admin.models';
import { MiSuscripcion } from '../../../core/models/suscripcion.models';

const ETIQUETAS_COBRO: Record<string, string> = {
  aprobado: 'Aprobado',
  rechazado: 'Rechazado',
  pendiente: 'En proceso',
  cancelado: 'Cancelado',
};

/** Pestaña "Suscripción" de Configuración: plan, próximo cobro, pagos y cambio de tarjeta. */
@Component({
  selector: 'app-mi-suscripcion',
  imports: [DatePipe, MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './mi-suscripcion.html',
  styleUrl: './mi-suscripcion.scss',
})
export class MiSuscripcionVista {
  readonly datos = input<MiSuscripcion | null>(null);
  readonly cargando = input(false);
  readonly error = input<string | null>(null);

  readonly cambiarTarjeta = output<void>();
  readonly reintentar = output<void>();

  dinero(valor: number): string {
    return '$' + Math.round(valor).toLocaleString('es-AR');
  }

  // Antes del primer cobro, si ese cobro es mayor al mensual, la diferencia es el alta: se avisa para que no parezca un error.
  // Devuelve null (no se muestra nada) si la suscripción no está activa, si el primer pago fue manual, si ya hubo un
  // cobro aprobado o si el próximo cobro no supera al mensual.
  montoAltaIncluida(s: MiSuscripcion): number | null {
    if (s.estado !== 'authorized' || s.primerPagoManual) return null;
    if (s.cobros.some((c) => c.estado === 'aprobado')) return null;
    const diferencia = (s.proximoMonto ?? 0) - s.montoMensual;
    return diferencia > 0 ? diferencia : null;
  }

  mesesDePromo(p: PromoSuscripcion): number[] {
    return Array.from({ length: p.mesesPromo }, (_, i) => i + 1);
  }

  etiquetaCobro(estado: string): string {
    return ETIQUETAS_COBRO[estado] ?? estado;
  }

  etiquetaEstado(s: MiSuscripcion): { texto: string; clase: string } {
    if (s.cobroRechazado) return { texto: 'Cobro rechazado', clase: 'chip-peligro' };
    switch (s.estado) {
      case 'authorized': return { texto: 'Activa', clase: 'chip-ok' };
      case 'paused': return { texto: 'Pausada', clase: 'chip-aviso' };
      case 'suspended': return { texto: 'Suspendida', clase: 'chip-aviso' };
      case 'cancelled': return { texto: 'Cancelada', clase: 'chip-gris' };
      default: return { texto: 'Pendiente', clase: 'chip-aviso' };
    }
  }
}
