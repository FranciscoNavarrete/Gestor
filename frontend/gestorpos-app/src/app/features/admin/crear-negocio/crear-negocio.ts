import { Clipboard } from '@angular/cdk/clipboard';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormGroupDirective, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { QrDialog } from '../../../core/dialogs/qr-dialog/qr-dialog';
import { AdminUsuario, CATALOGO_FEATURES, FluxoPlan, TenantResumen } from '../../../core/models/admin.models';
import { AdminAuthService } from '../../../core/services/admin-auth.service';
import { AdminService } from '../../../core/services/admin.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

@Component({
  selector: 'app-crear-negocio',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
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
export class CrearNegocio implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly clipboard = inject(Clipboard);
  protected readonly adminAuth = inject(AdminAuthService);

  readonly loginForm = this.fb.nonNullable.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });
  readonly loginError   = signal<string | null>(null);
  readonly iniciandoSesion = signal(false);

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
  readonly error = signal<string | null>(null);
  readonly ultimoCreado = signal<TenantResumen | null>(null);
  readonly ultimoCreadoCredenciales = signal<{ email: string; password: string } | null>(null);

  readonly negocios = signal<TenantResumen[]>([]);
  readonly cargandoNegocios = signal(false);
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

  ngOnInit(): void {
    if (!this.adminAuth.estaLogueado()) return;
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

  login(): void {
    if (this.loginForm.invalid || this.iniciandoSesion()) return;
    this.loginError.set(null);
    this.iniciandoSesion.set(true);

    const { email, password } = this.loginForm.getRawValue();
    this.adminAuth.login(email, password).subscribe({
      next: () => {
        this.iniciandoSesion.set(false);
        this.cargarNegocios();
        this.cargarPlanes();
        if (this.adminAuth.esOperador()) this.cargarVendedores();
      },
      error: () => {
        this.iniciandoSesion.set(false);
        this.loginError.set('Email o contraseña incorrectos.');
      },
    });
  }

  cargarNegocios(): void {
    this.cargandoNegocios.set(true);
    this.adminService.listarNegocios().subscribe({
      next: (negocios) => {
        this.negocios.set(negocios);
        this.cargandoNegocios.set(false);
      },
      error: (err) => {
        this.cargandoNegocios.set(false);
        this.manejarPosible401(err);
      },
    });
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

    this.guardando.set(true);
    this.error.set(null);
    this.ultimoCreado.set(null);
    this.ultimoCreadoCredenciales.set(null);

    const { email, password } = this.form.getRawValue();

    this.adminService.crearNegocio(this.form.getRawValue()).subscribe({
      next: (negocio) => {
        this.guardando.set(false);
        this.ultimoCreado.set(negocio);
        this.ultimoCreadoCredenciales.set({ email, password });
        this.formDirective?.resetForm();
        this.cargarNegocios();
      },
      error: (err) => {
        this.guardando.set(false);
        if (!this.manejarPosible401(err)) {
          this.error.set(extraerMensajeError(err));
        }
      },
    });
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

  logout(): void {
    this.adminAuth.logout();
    this.negocios.set([]);
    this.vendedores.set([]);
    this.planes.set([]);
    this.busqueda.set('');
    this.ultimoCreado.set(null);
    this.ultimoCreadoCredenciales.set(null);
    this.loginForm.reset();
  }

  // El logout + aviso + redirect ya los hace adminAuthInterceptor -- esto solo evita pisar ese
  // aviso con un mensaje de error genérico abajo del form.
  private manejarPosible401(err: unknown): boolean {
    return err instanceof HttpErrorResponse && err.status === 401;
  }
}
