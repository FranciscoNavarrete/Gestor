import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormBuilder, FormGroupDirective, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { CATALOGO_FEATURES, TenantResumen } from '../../../core/models/admin.models';
import { AdminAuthService } from '../../../core/services/admin-auth.service';
import { AdminService } from '../../../core/services/admin.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

@Component({
  selector: 'app-crear-negocio',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
  ],
  templateUrl: './crear-negocio.html',
  styleUrl: './crear-negocio.scss',
})
export class CrearNegocio implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);
  protected readonly adminAuth = inject(AdminAuthService);

  readonly claveForm = this.fb.nonNullable.group({ clave: ['', [Validators.required]] });
  readonly claveError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    nombreNegocio: ['', [Validators.required]],
    nombreAdmin: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  @ViewChild(FormGroupDirective) private formDirective?: FormGroupDirective;

  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly ultimoCreado = signal<TenantResumen | null>(null);

  readonly negocios = signal<TenantResumen[]>([]);
  readonly cargandoNegocios = signal(false);

  readonly catalogoFeatures = CATALOGO_FEATURES;
  readonly negocioExpandidoId = signal<string | null>(null);
  readonly cargandoFeatures = signal(false);
  readonly featuresActivos = signal<Set<string>>(new Set());
  readonly guardandoFeature = signal<string | null>(null);

  ngOnInit(): void {
    if (this.adminAuth.tieneClave()) this.cargarNegocios();
  }

  guardarClave(): void {
    if (this.claveForm.invalid) return;
    this.claveError.set(null);
    this.adminAuth.guardarClave(this.claveForm.getRawValue().clave);
    this.cargarNegocios();
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
        this.manejarPosibleClaveInvalida(err);
      },
    });
  }

  toggleExpandido(negocio: TenantResumen): void {
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

    this.adminService.crearNegocio(this.form.getRawValue()).subscribe({
      next: (negocio) => {
        this.guardando.set(false);
        this.ultimoCreado.set(negocio);
        this.formDirective?.resetForm();
        this.cargarNegocios();
      },
      error: (err) => {
        this.guardando.set(false);
        if (!this.manejarPosibleClaveInvalida(err)) {
          this.error.set(extraerMensajeError(err));
        }
      },
    });
  }

  cambiarClave(): void {
    this.adminAuth.olvidarClave();
    this.negocios.set([]);
    this.claveForm.reset();
  }

  private manejarPosibleClaveInvalida(err: unknown): boolean {
    if (err instanceof HttpErrorResponse && err.status === 401) {
      this.adminAuth.olvidarClave();
      this.claveError.set('La clave de administrador es incorrecta.');
      return true;
    }
    return false;
  }
}
