import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleChange, MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmDialog } from '../../../core/dialogs/confirm-dialog/confirm-dialog';
import { CambiarPlanDialog } from '../../../core/dialogs/cambiar-plan-dialog/cambiar-plan-dialog';
import { CobrosDialog } from '../../../core/dialogs/cobros-dialog/cobros-dialog';
import { TerminosRegistroDialog } from '../../../core/dialogs/terminos-registro-dialog/terminos-registro-dialog';
import { ConfirmarPagoDialog } from '../../../core/dialogs/confirmar-pago-dialog/confirmar-pago-dialog';
import { LinkPagoDialog } from '../../../core/dialogs/link-pago-dialog/link-pago-dialog';
import { CATALOGO_FEATURES, TenantResumen } from '../../../core/models/admin.models';
import { AdminAuthService } from '../../../core/services/admin-auth.service';
import { AdminService } from '../../../core/services/admin.service';
import { extraerMensajeError } from '../../../core/utils/error.util';
import { RefrescoAutomatico } from '../../../core/utils/refresco-automatico';

export type FiltroEstado = 'todos' | 'activos' | 'inactivos';

@Component({
  selector: 'app-negocios',
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
  ],
  templateUrl: './negocios.html',
  styleUrl: './negocios.scss',
})
export class Negocios implements OnInit, OnDestroy {
  private readonly adminService = inject(AdminService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  protected readonly adminAuth = inject(AdminAuthService);

  readonly negocios = signal<TenantResumen[]>([]);
  readonly cargandoNegocios = signal(false);
  readonly actualizando = signal(false);
  readonly ultimaActualizacion = signal<Date | null>(null);
  readonly busqueda = signal('');
  readonly filtroEstado = signal<FiltroEstado>('activos');
  readonly conteos = computed(() => {
    const todos = this.negocios();
    const activos = todos.filter((n) => n.activo).length;
    return { todos: todos.length, activos, inactivos: todos.length - activos };
  });
  readonly negociosFiltrados = computed(() => {
    const termino = this.busqueda().trim().toLowerCase();
    const estado = this.filtroEstado();
    return this.negocios().filter(
      (n) =>
        (estado === 'todos' || (estado === 'activos' ? n.activo : !n.activo)) &&
        (!termino || n.nombre.toLowerCase().includes(termino)),
    );
  });

  readonly catalogoFeatures = CATALOGO_FEATURES;
  readonly negocioExpandidoId = signal<string | null>(null);
  readonly cargandoFeatures = signal(false);
  readonly featuresActivos = signal<Set<string>>(new Set());
  readonly guardandoFeature = signal<string | null>(null);
  readonly cambiandoEstadoNegocioId = signal<string | null>(null);
  readonly refresco = new RefrescoAutomatico(() => this.cargarNegocios(true));

  ngOnInit(): void {
    this.cargarNegocios();
  }

  ngOnDestroy(): void {
    this.refresco.destruir();
  }

  cargarNegocios(silencioso = false): void {
    if (!silencioso) this.cargandoNegocios.set(true);
    this.actualizando.set(true);
    this.adminService.listarNegocios().subscribe({
      next: (negocios) => {
        this.negocios.set(negocios);
        this.cargandoNegocios.set(false);
        this.actualizando.set(false);
        this.ultimaActualizacion.set(new Date());
        this.refresco.evaluar(negocios.some((n) => n.fluxoEstado === 'pending'));
      },
      error: (err) => {
        this.cargandoNegocios.set(false);
        this.actualizando.set(false);
        this.manejarPosible401(err);
      },
    });
  }

  actualizarAhora(): void {
    this.refresco.reanudar();
    this.cargarNegocios(true);
  }

  toggleExpandido(negocio: TenantResumen): void {
    if (!this.adminAuth.esOperador()) return;
    if (this.negocioExpandidoId() === negocio.id) {
      this.negocioExpandidoId.set(null);
      return;
    }
    this.negocioExpandidoId.set(negocio.id);
    this.cargarFeatures(negocio.id);
  }

  private cargarFeatures(tenantId: string): void {
    this.cargandoFeatures.set(true);
    this.adminService.listarFeatures(tenantId).subscribe({
      next: (features) => {
        this.featuresActivos.set(new Set(features.filter((f) => f.habilitado).map((f) => f.clave)));
        this.cargandoFeatures.set(false);
      },
      error: () => this.cargandoFeatures.set(false),
    });
  }

  toggleFeature(negocio: TenantResumen, clave: string): void {
    if (this.guardandoFeature()) return;

    const activo = this.featuresActivos().has(clave);
    this.guardandoFeature.set(clave);

    const accion$ = activo
      ? this.adminService.desactivarFeature(negocio.id, clave)
      : this.adminService.activarFeature(negocio.id, clave);

    accion$.subscribe({
      next: () => {
        this.guardandoFeature.set(null);
        const nuevos = new Set(this.featuresActivos());
        if (activo) nuevos.delete(clave);
        else nuevos.add(clave);
        this.featuresActivos.set(nuevos);
      },
      error: () => this.guardandoFeature.set(null),
    });
  }

  verLinkPago(negocio: TenantResumen, event: Event): void {
    event.stopPropagation();
    this.dialog
      .open(LinkPagoDialog, { data: { tenantId: negocio.id, nombre: negocio.nombre }, width: '420px', maxWidth: '92vw' })
      .afterClosed()
      .subscribe(() => this.cargarNegocios(true));
  }

  private static readonly ESTADOS_SUSCRIPCION: Record<string, { texto: string; clase: string }> = {
    pending: { texto: 'Pendiente', clase: 'badge-pendiente' },
    authorized: { texto: 'Suscripto', clase: 'badge-suscripto' },
    paused: { texto: 'Pausado', clase: 'badge-pausado' },
    suspended: { texto: 'Suspendido', clase: 'badge-suspendido' },
    cancelled: { texto: 'Cancelado', clase: 'badge-cancelado' },
  };

  badgeSuscripcion(negocio: TenantResumen): { texto: string; clase: string } | null {
    if (!negocio.fluxoEstado) return null;
    // Primer pago por fuera de Mercado Pago: el negocio todavía no tiene acceso.
    if (negocio.formaPrimerPago && negocio.pendienteActivacion) {
      const esperaPago = negocio.pagoManualEstado === 'Pendiente';
      const esperaLink = negocio.fluxoEstado === 'pending';
      if (esperaPago) {
        return { texto: esperaLink ? 'Esperando transferencia y link' : 'Esperando transferencia', clase: 'badge-esperando' };
      }
      if (esperaLink) return { texto: 'Esperando que el cliente abra el link', clase: 'badge-pendiente' };
    }
    // Cancelada pero ya pagada: el negocio sigue usándose hasta la fecha de su próximo cobro.
    if (negocio.fluxoEstado === 'cancelled' && negocio.fluxoAccesoHasta) {
      const hasta = new Date(negocio.fluxoAccesoHasta).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', timeZone: 'America/Argentina/Buenos_Aires' });
      return { texto: `Cancelado · acceso hasta ${hasta}`, clase: 'badge-cancelado' };
    }
    if (negocio.fluxoEstado === 'authorized' && negocio.fluxoCobroRechazado) {
      return {
        texto: negocio.fluxoPrimerCobroAprobado ? 'Cobro rechazado' : '1er cobro rechazado',
        clase: 'badge-rechazado',
      };
    }
    if (negocio.formaPrimerPago && negocio.pagoManualEstado === 'Confirmado' && negocio.fluxoEstado === 'authorized') {
      const renueva = negocio.fluxoProximoCobro
        ? new Date(negocio.fluxoProximoCobro).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', timeZone: 'America/Argentina/Buenos_Aires' })
        : null;
      return { texto: renueva ? `Suscripto · renueva ${renueva}` : 'Suscripto', clase: 'badge-suscripto' };
    }
    if (negocio.fluxoEstado === 'authorized' && !negocio.fluxoPrimerCobroAprobado) {
      return { texto: 'Esperando 1er cobro', clase: 'badge-esperando' };
    }
    return Negocios.ESTADOS_SUSCRIPCION[negocio.fluxoEstado] ?? null;
  }

  // Una transferencia recibida la confirma el operador: recién ahí el negocio puede activarse.
  puedeConfirmarPago(negocio: TenantResumen): boolean {
    return this.adminAuth.esOperador() && negocio.pagoManualEstado === 'Pendiente';
  }

  confirmarPago(negocio: TenantResumen, event: Event): void {
    event.stopPropagation();
    this.dialog
      .open(ConfirmarPagoDialog, {
        data: {
          tenantId: negocio.id,
          nombre: negocio.nombre,
          monto: negocio.pagoManualMonto ?? 0,
          proximoCobro: negocio.fluxoProximoCobro ?? null,
          faltaLink: negocio.fluxoEstado === 'pending',
        },
        width: '440px',
        maxWidth: '94vw',
      })
      .afterClosed()
      .subscribe((confirmado) => {
        if (!confirmado) return;
        this.snackBar.open('Pago confirmado.', 'OK', { duration: 3000 });
        this.cargarNegocios(true);
      });
  }

  // Los cobros solo existen cuando la suscripción ya se autorizó alguna vez.
  tieneCobros(negocio: TenantResumen): boolean {
    return !!negocio.fluxoSuscripcionId && !!negocio.fluxoEstado && negocio.fluxoEstado !== 'pending';
  }

  // Solo el operador, y solo con la suscripción activa: el plan nuevo rige desde el próximo cobro.
  puedeCambiarPlan(negocio: TenantResumen): boolean {
    return this.adminAuth.esOperador() && negocio.fluxoEstado === 'authorized';
  }

  cambiarPlan(negocio: TenantResumen, event: Event): void {
    event.stopPropagation();
    this.dialog
      .open(CambiarPlanDialog, { data: { tenantId: negocio.id, nombre: negocio.nombre }, width: '480px', maxWidth: '94vw' })
      .afterClosed()
      .subscribe((cambiado) => {
        if (!cambiado) return;
        this.snackBar.open('Plan cambiado. Rige desde el próximo cobro.', 'OK', { duration: 3500 });
        this.cargarNegocios(true);
      });
  }

  verTerminos(negocio: TenantResumen, event: Event): void {
    event.stopPropagation();
    this.dialog.open(TerminosRegistroDialog, { data: { tenantId: negocio.id, nombre: negocio.nombre }, width: '420px', maxWidth: '92vw' });
  }

  verCobros(negocio: TenantResumen, event: Event): void {
    event.stopPropagation();
    this.dialog.open(CobrosDialog, { data: { tenantId: negocio.id, nombre: negocio.nombre }, width: '440px', maxWidth: '92vw' });
  }

  // El primer cobro ya se aprobó pero Fluxo no pudo bajar la suscripción al monto mensual todavía.
  ajusteMontoPendiente(negocio: TenantResumen): boolean {
    return this.adminAuth.esOperador() && negocio.fluxoAjustePendiente === true;
  }

  cambiarActivo(negocio: TenantResumen, cambio: MatSlideToggleChange): void {
    if (this.cambiandoEstadoNegocioId()) {
      cambio.source.checked = negocio.activo;
      return;
    }

    if (negocio.activo) {
      this.dialog
        .open(ConfirmDialog, {
          data: {
            titulo: 'Desactivar negocio',
            mensaje: `¿Desactivar "${negocio.nombre}"? El cliente pierde el acceso y el email queda libre para usarlo en otro negocio.`,
            textoConfirmar: 'Desactivar',
            peligroso: true,
          },
          width: '380px',
          maxWidth: '92vw',
        })
        .afterClosed()
        .subscribe((confirmado) => {
          if (!confirmado) {
            cambio.source.checked = true;
            return;
          }
          this.aplicarEstado(negocio, cambio, this.adminService.desactivarNegocio(negocio.id));
        });
      return;
    }

    this.aplicarEstado(negocio, cambio, this.adminService.activarNegocio(negocio.id));
  }

  private aplicarEstado(negocio: TenantResumen, cambio: MatSlideToggleChange, accion$: Observable<TenantResumen>): void {
    this.cambiandoEstadoNegocioId.set(negocio.id);
    accion$.subscribe({
      next: () => {
        this.cambiandoEstadoNegocioId.set(null);
        this.cargarNegocios(true);
      },
      error: (err) => {
        cambio.source.checked = negocio.activo;
        this.cambiandoEstadoNegocioId.set(null);
        this.snackBar.open(extraerMensajeError(err), 'OK', { duration: 6000 });
      },
    });
  }

  elegirFiltro(filtro: FiltroEstado): void {
    this.filtroEstado.set(filtro);
  }

  // El logout + aviso + redirect ya los hace adminAuthInterceptor -- esto solo evita pisar ese
  // aviso con un mensaje de error genérico.
  private manejarPosible401(err: unknown): boolean {
    return err instanceof HttpErrorResponse && err.status === 401;
  }
}
