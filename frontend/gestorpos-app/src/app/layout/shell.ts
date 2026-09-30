import { Component, ViewChild, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDialog } from '@angular/material/dialog';
import { MatMenuModule, MatMenuTrigger } from '@angular/material/menu';
import { ConfirmDialog } from '../core/dialogs/confirm-dialog/confirm-dialog';
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
  private readonly dialog = inject(MatDialog);
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

  formatMonto(valor: number): string {
    return `$${Math.round(valor).toLocaleString('es-AR')}`;
  }

  irAVender(): void {
    this.carritoTrigger?.closeMenu();
    this.router.navigate(['/venta']);
  }

  cancelarVentaEnCurso(): void {
    this.carritoTrigger?.closeMenu();
    this.dialog
      .open(ConfirmDialog, {
        data: {
          titulo: 'Cancelar venta en curso',
          mensaje: 'Se va a vaciar el carrito y perdés lo que llevás cargado.',
          textoConfirmar: 'Sí, cancelar',
          peligroso: true,
        },
        width: '360px',
      })
      .afterClosed()
      .subscribe((confirmado) => {
        if (confirmado) this.carritoEnCurso.cancelar();
      });
  }

  cerrarSesion(): void {
    this.dialog
      .open(ConfirmDialog, {
        data: { titulo: 'Cerrar sesión', mensaje: '¿Seguro que querés salir?', textoConfirmar: 'Cerrar sesión' },
        width: '360px',
      })
      .afterClosed()
      .subscribe((confirmado) => {
        if (!confirmado) return;
        this.authService.logout();
        this.router.navigate(['/login']);
      });
  }
}
