import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, MatToolbarModule, MatIconModule, MatButtonModule],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly nombreNegocio = this.authService.nombreNegocio;
  readonly nombreUsuario = this.authService.nombreUsuario;

  readonly navItems = [
    { path: '/dashboard', icon: 'dashboard', label: 'Resumen' },
    { path: '/venta', icon: 'point_of_sale', label: 'Vender' },
    { path: '/productos', icon: 'inventory_2', label: 'Productos' },
    { path: '/caja', icon: 'payments', label: 'Caja' },
    { path: '/reportes', icon: 'bar_chart', label: 'Reportes' },
  ];

  cerrarSesion(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
