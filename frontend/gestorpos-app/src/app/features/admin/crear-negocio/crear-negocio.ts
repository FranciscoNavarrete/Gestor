import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormBuilder, FormGroupDirective, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TenantResumen } from '../../../core/models/admin.models';
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
    MatListModule,
    MatProgressSpinnerModule,
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
