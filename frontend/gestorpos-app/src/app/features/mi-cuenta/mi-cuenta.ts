import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CambiarPassword } from '../../core/components/cambiar-password/cambiar-password';
import { AuthService } from '../../core/services/auth.service';

/** Los datos de la cuenta de quien está logueado y el cambio de su contraseña. */
@Component({
  selector: 'app-mi-cuenta',
  imports: [MatButtonModule, MatCardModule, MatIconModule, CambiarPassword],
  templateUrl: './mi-cuenta.html',
  styleUrl: './mi-cuenta.scss',
})
export class MiCuenta {
  private readonly authService = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);

  readonly nombreUsuario = this.authService.nombreUsuario;
  readonly nombreNegocio = this.authService.nombreNegocio;
  readonly rol = this.authService.auth;
  readonly cambiando = signal(false);
  private readonly formulario = viewChild<ElementRef<HTMLElement>>('formulario');

  // En celular el formulario queda debajo de la barra de abajo: al abrirlo se lo trae a la vista completo.
  abrirFormulario(): void {
    this.cambiando.set(true);
    setTimeout(() => this.formulario()?.nativeElement.scrollIntoView({ block: 'end', behavior: 'smooth' }));
  }

  readonly guardar = (actual: string, nueva: string) => this.authService.cambiarPassword(actual, nueva);

  alCambiar(): void {
    this.cambiando.set(false);
    this.snackBar.open('Contraseña cambiada', 'OK', { duration: 3000 });
  }
}
