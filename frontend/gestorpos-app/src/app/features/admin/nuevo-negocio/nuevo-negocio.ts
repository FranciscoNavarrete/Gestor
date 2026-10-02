import { Clipboard } from '@angular/cdk/clipboard';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormBuilder, FormGroupDirective, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { QrDialog } from '../../../core/dialogs/qr-dialog/qr-dialog';
import { AdminUsuario, FluxoPlan, TenantResumen } from '../../../core/models/admin.models';
import { AdminAuthService } from '../../../core/services/admin-auth.service';
import { AdminService } from '../../../core/services/admin.service';
import { MpService } from '../../../core/services/mp.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

@Component({
  selector: 'app-nuevo-negocio',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './nuevo-negocio.html',
  styleUrl: './nuevo-negocio.scss',
})
export class NuevoNegocio implements OnInit, OnDestroy {
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

  ngOnInit(): void {
    this.cargarPlanes();
    if (this.adminAuth.esOperador()) this.cargarVendedores();
  }

  ngOnDestroy(): void {
    this.mp.unmountBrick();
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
  }

  cerrarResultado(): void {
    this.ultimoCreado.set(null);
    this.ultimoCreadoCredenciales.set(null);
  }

  verNegocios(): void {
    this.router.navigateByUrl('/admin/negocios');
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

  // El logout + aviso + redirect ya los hace adminAuthInterceptor -- esto solo evita pisar ese
  // aviso con un mensaje de error genérico abajo del form.
  private manejarPosible401(err: unknown): boolean {
    return err instanceof HttpErrorResponse && err.status === 401;
  }
}
