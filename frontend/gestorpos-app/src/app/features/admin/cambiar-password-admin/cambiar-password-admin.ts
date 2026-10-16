import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { CambiarPassword } from '../../../core/components/cambiar-password/cambiar-password';
import { AdminAuthService } from '../../../core/services/admin-auth.service';

/** Pantalla a la que entra quien ingresó al panel interno con una contraseña temporal. */
@Component({
  selector: 'app-cambiar-password-admin',
  imports: [MatCardModule, CambiarPassword],
  templateUrl: './cambiar-password-admin.html',
  styleUrl: '../../auth/login/login.scss',
})
export class CambiarPasswordAdmin {
  private readonly adminAuth = inject(AdminAuthService);
  private readonly router = inject(Router);

  readonly guardar = (actual: string, nueva: string) => this.adminAuth.cambiarPassword(actual, nueva);

  alCambiar(): void {
    this.router.navigateByUrl('/admin/negocios');
  }

  cerrarSesion(): void {
    this.adminAuth.logout();
    this.router.navigateByUrl('/admin/login');
  }
}
