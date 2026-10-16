import { Clipboard } from '@angular/cdk/clipboard';
import { Component, ViewChild, inject, signal } from '@angular/core';
import { FormBuilder, FormGroupDirective, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TenantResumen } from '../../../core/models/admin.models';
import { AdminService } from '../../../core/services/admin.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

/** Alta de un negocio sin cobro ni vendedor (solo operador): p. ej. el segundo local de un cliente que paga doble suscripción. */
@Component({
  selector: 'app-alta-sin-cobro',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './alta-sin-cobro.html',
  styleUrl: './alta-sin-cobro.scss',
})
export class AltaSinCobro {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly clipboard = inject(Clipboard);

  readonly form = this.fb.nonNullable.group({
    nombreNegocio: ['', [Validators.required]],
    nombreAdmin: ['', [Validators.required]],
    telefono: ['', [Validators.required, Validators.pattern(/^(?:\D*\d){8,}\D*$/)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    nota: [''],
  });

  @ViewChild(FormGroupDirective) private formDirective?: FormGroupDirective;

  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly creado = signal<TenantResumen | null>(null);
  readonly credenciales = signal<{ email: string; password: string } | null>(null);

  crear(): void {
    if (this.form.invalid || this.guardando()) return;
    this.guardando.set(true);
    this.error.set(null);

    const { nota, ...resto } = this.form.getRawValue();
    this.adminService.crearNegocioSinCobro({ ...resto, nota: nota.trim() || null }).subscribe({
      next: (negocio) => {
        this.guardando.set(false);
        this.creado.set(negocio);
        this.credenciales.set({ email: resto.email, password: resto.password });
        this.formDirective?.resetForm();
      },
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }

  copiar(texto: string, etiqueta: string): void {
    this.clipboard.copy(texto);
    this.snackBar.open(`${etiqueta} copiado`, 'OK', { duration: 2500 });
  }

  cerrarResultado(): void {
    this.creado.set(null);
    this.credenciales.set(null);
  }

  verNegocios(): void {
    this.router.navigate(['/admin/negocios']);
  }
}
