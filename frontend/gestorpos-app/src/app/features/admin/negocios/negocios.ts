import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleChange, MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmDialog } from '../../../core/dialogs/confirm-dialog/confirm-dialog';
import { NuevaSuscripcionDialog } from '../../../core/dialogs/nueva-suscripcion-dialog/nueva-suscripcion-dialog';
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

export type FiltroEstado = 'todos' | 'activos' | 'inactivos' | 'porConfirmar' | 'sinAutorizar';

const FILTROS_VALIDOS: FiltroEstado[] = ['todos', 'activos', 'inactivos', 'porConfirmar', 'sinAutorizar'];

@Component({
  selector: 'app-negocios',
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
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
  private readonly route = inject(ActivatedRoute);
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
    return {
      todos: todos.length,
      activos,
      inactivos: todos.length - activos,
      porConfirmar: todos.filter((n) => this.esPorConfirmar(n)).length,
      sinAutorizar: todos.filter((n) => this.esSinAutorizar(n)).length,
    };
  });
  readonly negociosFiltrados = computed(() => {
    const termino = this.busqueda().trim().toLowerCase();
    const estado = this.filtroEstado();
    return this.negocios().filter(
      (n) => this.cumpleFiltro(n, estado) && (!termino || n.nombre.toLowerCase().includes(termino)),
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
    // Desde el Resumen se llega con el filtro ya elegido (?filtro=porConfirmar).
    const pedido = this.route.snapshot.queryParamMap.get('filtro') as FiltroEstado | null;
    if (pedido && FILTROS_VALIDOS.includes(pedido)) this.filtroEstado.set(pedido);
    this.cargarNegocios();
  }

  // Una transferencia (o tarjeta u otro) recibida que el operador todavía no confirmó.
  esPorConfirmar(negocio: TenantResumen): boolean {
    return negocio.pagoManualEstado === 'Pendiente';
  }

  // El cliente todavía no abrió el link de pago ni autorizó su suscripción.
  esSinAutorizar(negocio: TenantResumen): boolean {
    return negocio.fluxoEstado === 'pending';
  }

  private cumpleFiltro(negocio: TenantResumen, filtro: FiltroEstado): boolean {
    switch (filtro) {
      case 'activos': return negocio.activo;
      case 'inactivos': return !negocio.activo;
      case 'porConfirmar': return this.esPorConfirmar(negocio);
      case 'sinAutorizar': return this.esSinAutorizar(negocio);
      default: return true;
    }
  }

  // Alta o baja desde el menú de la tarjeta (en celular no hay interruptor).
  alternarActivo(negocio: TenantResumen): void {
    this.cambiarActivo(negocio);
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

  // Un negocio que ya perdió el acceso porque su suscripción se canceló (o que nunca tuvo una) se puede volver a suscribir.
  puedeNuevaSuscripcion(negocio: TenantResumen): boolean {
    return !negocio.activo && !negocio.pendienteActivacion
      && (!negocio.fluxoSuscripcionId || negocio.fluxoEstado === 'cancelled');
  }

  nuevaSuscripcion(negocio: TenantResumen, event: Event): void {
    event.stopPropagation();
    this.dialog
      .open(NuevaSuscripcionDialog, {
        data: { tenantId: negocio.id, nombre: negocio.nombre, emailAdmin: negocio.emailAdmin ?? null },
        width: '520px',
        maxWidth: '94vw',
      })
      .afterClosed()
      .subscribe((creado: TenantResumen | null | undefined) => {
        if (!creado) return;
        this.cargarNegocios(true);
        if (creado.fluxoInitPoint) {
          // Por link: se muestra enseguida para pasárselo al cliente; el acceso vuelve cuando lo autorice.
          this.dialog.open(LinkPagoDialog, { data: { tenantId: creado.id, nombre: creado.nombre }, width: '420px', maxWidth: '92vw' });
        } else {
          this.snackBar.open('Suscripción creada. El negocio ya tiene acceso.', 'OK', { duration: 4000 });
        }
      });
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

  cambiarActivo(negocio: TenantResumen, cambio?: MatSlideToggleChange): void {
    if (this.cambiandoEstadoNegocioId()) {
      if (cambio) cambio.source.checked = negocio.activo;
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
            if (cambio) cambio.source.checked = true;
            return;
          }
          this.aplicarEstado(negocio, cambio, this.adminService.desactivarNegocio(negocio.id));
        });
      return;
    }

    this.aplicarEstado(negocio, cambio, this.adminService.activarNegocio(negocio.id));
  }

  private aplicarEstado(negocio: TenantResumen, cambio: MatSlideToggleChange | undefined, accion$: Observable<TenantResumen>): void {
    this.cambiandoEstadoNegocioId.set(negocio.id);
    accion$.subscribe({
      next: () => {
        this.cambiandoEstadoNegocioId.set(null);
        this.cargarNegocios(true);
      },
      error: (err) => {
        if (cambio) cambio.source.checked = negocio.activo;
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
