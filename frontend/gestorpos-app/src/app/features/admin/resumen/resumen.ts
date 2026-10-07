import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ClienteEnRiesgo, ResumenFinanciero, TenantResumen } from '../../../core/models/admin.models';
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
  private readonly router = inject(Router);

  readonly resumen = signal<ResumenFinanciero | null>(null);
  readonly cargando = signal(false);
  readonly error = signal<string | null>(null);
  readonly actualizado = signal<Date | null>(null);

  // Lo que espera una acción del operador: pagos por confirmar y suscripciones cuyo link nadie abrió.
  readonly negocios = signal<TenantResumen[]>([]);
  readonly pendientes = computed(() => {
    const todos = this.negocios();
    const porConfirmar = todos.filter((n) => n.pagoManualEstado === 'Pendiente');
    return {
      porConfirmar: porConfirmar.length,
      montoPorConfirmar: porConfirmar.reduce((suma, n) => suma + (n.pagoManualMonto ?? 0), 0),
      sinAutorizar: todos.filter((n) => n.fluxoEstado === 'pending').length,
    };
  });

  readonly maximoAltas = computed(() => Math.max(1, ...(this.resumen()?.altasPorMes ?? []).map((a) => a.altas)));
  readonly mesActual = new Date().getMonth() + 1;

  ngOnInit(): void {
    this.cargar();
  }

  irANegocios(filtro: 'porConfirmar' | 'sinAutorizar'): void {
    void this.router.navigate(['/admin/negocios'], { queryParams: { filtro } });
  }

  cargar(): void {
    // Un fallo acá no debe tapar el resto del resumen: simplemente no se muestran los pendientes.
    this.adminService.listarNegocios().subscribe({
      next: (negocios) => this.negocios.set(negocios),
      error: () => this.negocios.set([]),
    });
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
