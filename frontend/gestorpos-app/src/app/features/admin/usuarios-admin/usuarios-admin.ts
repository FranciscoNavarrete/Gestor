import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ViewChild, inject, signal } from '@angular/core';
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
import { RestablecerPasswordDialog } from '../../../core/dialogs/restablecer-password-dialog/restablecer-password-dialog';
import { AdminRol, AdminUsuario } from '../../../core/models/admin.models';
import { AdminAuthService } from '../../../core/services/admin-auth.service';
import { AdminService } from '../../../core/services/admin.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

@Component({
  selector: 'app-usuarios-admin',
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
  ],
  templateUrl: './usuarios-admin.html',
  styleUrl: './usuarios-admin.scss',
})
export class UsuariosAdmin implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);
  private readonly dialog = inject(MatDialog);
  protected readonly adminAuth = inject(AdminAuthService);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    nombre: ['', [Validators.required]],
    rol: this.fb.nonNullable.control<AdminRol>('Vendedor'),
  });

  @ViewChild(FormGroupDirective) private formDirective?: FormGroupDirective;

  readonly usuarios = signal<AdminUsuario[]>([]);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly cambiandoEstadoId = signal<string | null>(null);

  ngOnInit(): void {
    if (this.adminAuth.estaLogueado() && this.adminAuth.esOperador()) this.cargarUsuarios();
  }

  cargarUsuarios(): void {
    this.cargando.set(true);
    this.adminService.listarUsuarios().subscribe({
      next: (usuarios) => {
        this.usuarios.set(usuarios);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }

  crear(): void {
    if (this.form.invalid || this.guardando()) return;

    this.guardando.set(true);
    this.error.set(null);

    this.adminService.crearUsuario(this.form.getRawValue()).subscribe({
      next: () => {
        this.guardando.set(false);
        this.formDirective?.resetForm({ rol: 'Vendedor' });
        this.cargarUsuarios();
      },
      error: (err) => {
        this.guardando.set(false);
        // adminAuthInterceptor ya desloguea + avisa + redirige en un 401 -- no pisarlo acá
        // con el mensaje de error genérico.
        if (!(err instanceof HttpErrorResponse && err.status === 401)) {
          this.error.set(extraerMensajeError(err));
        }
      },
    });
  }

  // El operador le restablece la contraseña a un vendedor u otro operador con una temporal.
  restablecerPassword(usuario: AdminUsuario): void {
    this.dialog.open(RestablecerPasswordDialog, {
      data: { tipo: 'usuario', id: usuario.id, nombre: usuario.nombre },
      width: '440px',
      maxWidth: '94vw',
    });
  }

  toggleEstado(usuario: AdminUsuario): void {
    if (this.cambiandoEstadoId()) return;
    this.cambiandoEstadoId.set(usuario.id);

    const accion$ = usuario.activo
      ? this.adminService.desactivarUsuario(usuario.id)
      : this.adminService.activarUsuario(usuario.id);

    accion$.subscribe({
      next: () => {
        this.cambiandoEstadoId.set(null);
        this.cargarUsuarios();
      },
      error: () => this.cambiandoEstadoId.set(null),
    });
  }
}
