import { DatePipe } from '@angular/common';
import { Clipboard } from '@angular/cdk/clipboard';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormGroupDirective, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { LinkPagoDialog } from '../../../core/dialogs/link-pago-dialog/link-pago-dialog';
import { QrDialog } from '../../../core/dialogs/qr-dialog/qr-dialog';
import { AdminUsuario, CATALOGO_FEATURES, FluxoPlan, TenantResumen } from '../../../core/models/admin.models';
import { AdminAuthService } from '../../../core/services/admin-auth.service';
import { AdminService } from '../../../core/services/admin.service';
import { MpService } from '../../../core/services/mp.service';
import { extraerMensajeError } from '../../../core/utils/error.util';
import { RefrescoAutomatico } from '../../../core/utils/refresco-automatico';

@Component({
  selector: 'app-crear-negocio',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSlideToggleModule,
  ],
  templateUrl: './crear-negocio.html',
  styleUrl: './crear-negocio.scss',
})
export class CrearNegocio implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly clipboard = inject(Clipboard);
  private readonly mp = inject(MpService);
  protected readonly adminAuth = inject(AdminAuthService);

  readonly form = this.fb.nonNullable.group({
    nombreNegocio: ['', [Validators.required]],
    nombreAdmin: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    vendedorId: this.fb.control<string | null>(null),
    mpPlanId: this.fb.control<number | null>(null, [Validators.required]),
  });

  readonly vendedores = signal<AdminUsuario[]>([]);
  readonly planes = signal<FluxoPlan[]>([]);

  @ViewChild(FormGroupDirective) private formDirective?: FormGroupDirective;

  readonly guardando = signal(false);
  readonly metodoCobro = signal<'link' | 'tarjeta'>('link');
  readonly tarjetaActiva = signal(false);
  readonly error = signal<string | null>(null);
  readonly ultimoCreado = signal<TenantResumen | null>(null);
  readonly ultimoCreadoCredenciales = signal<{ email: string; password: string } | null>(null);

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

  ngOnDestroy(): void {
    this.mp.unmountBrick();
    this.refresco.destruir();
  }

  ngOnInit(): void {
    this.cargarNegocios();
    this.cargarPlanes();
    if (this.adminAuth.esOperador()) this.cargarVendedores();
  }

  private cargarVendedores(): void {
    this.adminService.listarUsuarios().subscribe({
      next: (usuarios) => this.vendedores.set(usuarios.filter((u) => u.rol === 'Vendedor' && u.activo)),
      error: () => {},
    });
  }

  private cargarPlanes(): void {
    this.adminService.listarPlanesFluxo().subscribe({
      next: (planes) => this.planes.set(planes),
      error: () => {},
    });
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

  crear(): void {
    if (this.form.invalid || this.guardando()) return;

    if (this.metodoCobro() === 'tarjeta') {
      void this.continuarConTarjeta();
      return;
    }

    this.guardando.set(true);
    this.error.set(null);
    this.ultimoCreado.set(null);
    this.ultimoCreadoCredenciales.set(null);

    const { email, password } = this.form.getRawValue();

    this.adminService.crearNegocio(this.form.getRawValue()).subscribe({
      next: (negocio) => {
        this.guardando.set(false);
        this.alCrearse(negocio, email, password);
      },
      error: (err) => {
        this.guardando.set(false);
        if (!this.manejarPosible401(err)) {
          this.error.set(extraerMensajeError(err));
        }
      },
    });
  }

  elegirMetodoCobro(metodo: 'link' | 'tarjeta'): void {
    if (this.guardando() || this.tarjetaActiva()) return;
    this.metodoCobro.set(metodo);
  }

  async continuarConTarjeta(): Promise<void> {
    if (this.form.invalid || this.guardando()) return;

    const { email, mpPlanId } = this.form.getRawValue();
    const plan = this.planes().find((p) => p.mpPlanId === mpPlanId);
    if (!plan) {
      this.error.set('Elegí un plan para cobrar con tarjeta.');
      return;
    }

    this.error.set(null);
    this.ultimoCreado.set(null);
    this.ultimoCreadoCredenciales.set(null);
    this.form.disable();
    this.tarjetaActiva.set(true);

    try {
      await this.mp.mountCardPaymentBrick({
        containerId: 'brick-tarjeta',
        amount: plan.monto,
        emailPagador: email,
        submitLabel: 'Crear negocio y cobrar',
        onSubmit: (data) => this.crearConTarjeta(data.token),
        onError: () => this.error.set('Mercado Pago no pudo procesar los datos de la tarjeta. Revisalos y probá de nuevo.'),
      });
    } catch {
      this.error.set('No se pudo cargar el formulario de tarjeta de Mercado Pago. Revisá la conexión y probá de nuevo.');
      this.cancelarTarjeta();
    }
  }

  cancelarTarjeta(): void {
    this.mp.unmountBrick();
    this.tarjetaActiva.set(false);
    this.form.enable();
  }

  private crearConTarjeta(cardToken: string): Promise<void> {
    if (this.guardando()) return Promise.reject();

    this.guardando.set(true);
    this.error.set(null);
    const request = { ...this.form.getRawValue(), cardToken };

    return new Promise<void>((resolve, reject) => {
      this.adminService.crearNegocio(request).subscribe({
        next: (negocio) => {
          this.guardando.set(false);
          this.alCrearse(negocio, request.email, request.password);
          this.cancelarTarjeta();
          resolve();
        },
        error: (err) => {
          this.guardando.set(false);
          if (!this.manejarPosible401(err)) {
            this.error.set(extraerMensajeError(err));
          }
          reject();
        },
      });
    });
  }

  private alCrearse(negocio: TenantResumen, email: string, password: string): void {
    this.ultimoCreado.set(negocio);
    this.ultimoCreadoCredenciales.set({ email, password });
    this.formDirective?.resetForm();
    this.cargarNegocios();
  }

  cerrarResultado(): void {
    this.ultimoCreado.set(null);
    this.ultimoCreadoCredenciales.set(null);
  }

  verLinkPago(negocio: TenantResumen, event: Event): void {
    event.stopPropagation();
    this.dialog
      .open(LinkPagoDialog, { data: { tenantId: negocio.id, nombre: negocio.nombre }, width: '420px', maxWidth: '92vw' })
      .afterClosed()
      .subscribe(() => this.cargarNegocios(true));
  }

  abrirQr(): void {
    const creado = this.ultimoCreado();
    if (!creado?.fluxoInitPoint) return;
    this.dialog.open(QrDialog, {
      data: { nombre: creado.nombre, link: creado.fluxoInitPoint },
      width: '300px',
    });
  }

  copiar(texto: string, etiqueta: string): void {
    this.clipboard.copy(texto);
    this.snackBar.open(`${etiqueta} copiado`, 'OK', { duration: 2500 });
  }

  linkWhatsapp(creado: TenantResumen): string {
    const mensaje = `Hola! Para suscribirte a ${creado.nombre}, entrá acá: ${creado.fluxoInitPoint}`;
    return `https://wa.me/?text=${encodeURIComponent(mensaje)}`;
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
    return CrearNegocio.ESTADOS_SUSCRIPCION[negocio.fluxoEstado] ?? null;
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

  logout(): void {
    if (!confirm('¿Cerrar sesión?')) return;
    this.adminAuth.logout();
    this.router.navigateByUrl('/admin/login');
  }

  // El logout + aviso + redirect ya los hace adminAuthInterceptor -- esto solo evita pisar ese
  // aviso con un mensaje de error genérico abajo del form.
  private manejarPosible401(err: unknown): boolean {
    return err instanceof HttpErrorResponse && err.status === 401;
  }
}
