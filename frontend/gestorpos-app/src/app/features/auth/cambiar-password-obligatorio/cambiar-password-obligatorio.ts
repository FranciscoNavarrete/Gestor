import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { CambiarPassword } from '../../../core/components/cambiar-password/cambiar-password';
import { AuthService } from '../../../core/services/auth.service';

/** Pantalla a la que entra quien ingresó con una contraseña temporal: tiene que elegir una nueva antes de seguir. */
@Component({
  selector: 'app-cambiar-password-obligatorio',
  imports: [MatCardModule, CambiarPassword],
  templateUrl: './cambiar-password-obligatorio.html',
  styleUrl: '../login/login.scss',
})
export class CambiarPasswordObligatorio {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly guardar = (actual: string, nueva: string) => this.authService.cambiarPassword(actual, nueva);

  alCambiar(): void {
    this.router.navigate(['/dashboard']);
  }

  cerrarSesion(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
