import { Component, ViewChild, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule, MatMenuTrigger } from '@angular/material/menu';
import { FEATURE_MENU_LATERAL } from '../core/models/menu-lateral';
import { FEATURE_TAREAS } from '../core/models/tareas-feature';
import { AuthService } from '../core/services/auth.service';
import { CarritoEnCursoService } from '../core/services/carrito-en-curso.service';

@Component({
  selector: 'app-shell',
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatBadgeModule,
    MatMenuModule,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  readonly carritoEnCurso = inject(CarritoEnCursoService);

  @ViewChild('carritoTrigger') private carritoTrigger?: MatMenuTrigger;

  readonly nombreNegocio = this.authService.nombreNegocio;
  readonly nombreUsuario = this.authService.nombreUsuario;
  readonly tieneMenuLateral = this.authService.tieneFeature(FEATURE_MENU_LATERAL);
  readonly tieneTareas = this.authService.tieneFeature(FEATURE_TAREAS);

  readonly navItems = [
    { path: '/dashboard', icon: 'dashboard', label: 'Resumen' },
    { path: '/venta', icon: 'point_of_sale', label: 'Vender' },
    { path: '/productos', icon: 'inventory_2', label: 'Productos' },
    { path: '/caja', icon: 'payments', label: 'Caja' },
    { path: '/reportes', icon: 'bar_chart', label: 'Reportes' },
  ];

  irAVender(): void {
    this.carritoTrigger?.closeMenu();
    this.router.navigate(['/venta']);
  }

  cancelarVentaEnCurso(): void {
    if (!confirm('¿Cancelar la venta en curso? Se va a vaciar el carrito.')) return;
    this.carritoTrigger?.closeMenu();
    this.carritoEnCurso.cancelar();
  }

  cerrarSesion(): void {
    if (!confirm('¿Cerrar sesión?')) return;

    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
