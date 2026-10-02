import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { LinkPagoDialog } from '../../../core/dialogs/link-pago-dialog/link-pago-dialog';
import { CATALOGO_FEATURES, TenantResumen } from '../../../core/models/admin.models';
import { AdminAuthService } from '../../../core/services/admin-auth.service';
import { AdminService } from '../../../core/services/admin.service';
import { RefrescoAutomatico } from '../../../core/utils/refresco-automatico';

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
  protected readonly adminAuth = inject(AdminAuthService);

  readonly negocios = signal<TenantResumen[]>([]);
  readonly cargandoNegocios = signal(false);
  readonly actualizando = signal(false);
  readonly ultimaActualizacion = signal<Date | null>(null);
  readonly busqueda = signal('');
  readonly negociosFiltrados = computed(() => {
    const termino = this.busqueda().trim().toLowerCase();
    if (!termino) return this.negocios();
    return this.negocios().filter((n) => n.nombre.toLowerCase().includes(termino));
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
    return Negocios.ESTADOS_SUSCRIPCION[negocio.fluxoEstado] ?? null;
  }

  toggleEstadoNegocio(negocio: TenantResumen, event: Event): void {
    event.stopPropagation();
    if (this.cambiandoEstadoNegocioId()) return;

    if (negocio.activo) {
      const confirmado = confirm(
        `¿Desactivar "${negocio.nombre}"? El cliente pierde el acceso y el email queda libre para usarlo en otro negocio.`,
      );
      if (!confirmado) return;
    }

    this.cambiandoEstadoNegocioId.set(negocio.id);
    const accion$ = negocio.activo
      ? this.adminService.desactivarNegocio(negocio.id)
      : this.adminService.activarNegocio(negocio.id);

    accion$.subscribe({
      next: () => {
        this.cambiandoEstadoNegocioId.set(null);
        this.cargarNegocios();
      },
      error: () => this.cambiandoEstadoNegocioId.set(null),
    });
  }

  // El logout + aviso + redirect ya los hace adminAuthInterceptor -- esto solo evita pisar ese
  // aviso con un mensaje de error genérico.
  private manejarPosible401(err: unknown): boolean {
    return err instanceof HttpErrorResponse && err.status === 401;
  }
}
