import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ClienteEnRiesgo, ResumenFinanciero } from '../../../core/models/admin.models';
import { AdminService } from '../../../core/services/admin.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

const MESES = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

const ETIQUETAS_RIESGO: Record<ClienteEnRiesgo['tipo'], string> = {
  'primer-cobro': '1er cobro',
  rechazado: 'Rechazado',
  pausado: 'Pausado',
  suspendido: 'Suspendido',
};

@Component({
  selector: 'app-resumen-admin',
  imports: [DatePipe, MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './resumen.html',
  styleUrl: './resumen.scss',
})
export class ResumenAdmin implements OnInit {
  private readonly adminService = inject(AdminService);

  readonly resumen = signal<ResumenFinanciero | null>(null);
  readonly cargando = signal(false);
  readonly error = signal<string | null>(null);
  readonly actualizado = signal<Date | null>(null);

  readonly maximoAltas = computed(() => Math.max(1, ...(this.resumen()?.altasPorMes ?? []).map((a) => a.altas)));
  readonly mesActual = new Date().getMonth() + 1;

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.adminService.obtenerResumen().subscribe({
      next: (resumen) => {
        this.resumen.set(resumen);
        this.actualizado.set(new Date());
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        this.resumen.set(null);
        this.error.set(extraerMensajeError(err));
      },
    });
  }

  dinero(valor: number): string {
    return '$' + Math.round(valor).toLocaleString('es-AR');
  }

  nombreMes(mes: number): string {
    return MESES[mes - 1];
  }

  etiquetaRiesgo(tipo: ClienteEnRiesgo['tipo']): string {
    return ETIQUETAS_RIESGO[tipo];
  }

  motivo(c: ClienteEnRiesgo): string {
    if (c.motivo) return c.motivo;
    if (c.tipo === 'pausado') return 'Cobros pausados en Mercado Pago';
    if (c.tipo === 'suspendido') return 'Mercado Pago no pudo cobrarle varias veces';
    return 'Cobro rechazado';
  }
}
