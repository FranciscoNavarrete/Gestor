import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { filter } from 'rxjs';
import { ConfirmDialog } from '../../../core/dialogs/confirm-dialog/confirm-dialog';
import { AdminAuthService } from '../../../core/services/admin-auth.service';

interface ItemNavegacion {
  path: string;
  icono: string;
  etiqueta: string;
  etiquetaCorta: string;
}

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatButtonModule, MatIconModule, MatMenuModule],
  templateUrl: './admin-shell.html',
  styleUrl: './admin-shell.scss',
})
export class AdminShell {
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  protected readonly adminAuth = inject(AdminAuthService);

  readonly titulo = signal(this.leerTitulo());

  readonly items = computed<ItemNavegacion[]>(() => {
    const items: ItemNavegacion[] = [
      { path: '/admin/negocios', icono: 'storefront', etiqueta: 'Negocios', etiquetaCorta: 'Negocios' },
      { path: '/admin/nuevo-negocio', icono: 'add_circle', etiqueta: 'Nuevo negocio', etiquetaCorta: 'Nuevo' },
      { path: '/admin/reportes', icono: 'bar_chart', etiqueta: 'Reportes', etiquetaCorta: 'Reportes' },
    ];
    if (this.adminAuth.esOperador()) {
      items.unshift({ path: '/admin/resumen', icono: 'insights', etiqueta: 'Resumen', etiquetaCorta: 'Resumen' });
      items.push({ path: '/admin/movimientos', icono: 'history', etiqueta: 'Movimientos', etiquetaCorta: 'Historial' });
      items.push({ path: '/admin/usuarios', icono: 'group', etiqueta: 'Usuarios', etiquetaCorta: 'Usuarios' });
    }
    return items;
  });

  readonly inicial = computed(() => (this.adminAuth.nombre() || '?').charAt(0).toUpperCase());

  constructor() {
    this.router.events
      .pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.titulo.set(this.leerTitulo()));
  }

  cerrarSesion(): void {
    this.dialog
      .open(ConfirmDialog, {
        data: {
          titulo: 'Cerrar sesión',
          mensaje: 'Vas a tener que volver a ingresar con tu usuario y contraseña.',
          textoConfirmar: 'Cerrar sesión',
        },
        width: '360px',
        maxWidth: '92vw',
      })
      .afterClosed()
      .subscribe((confirmado) => {
        if (!confirmado) return;
        this.adminAuth.logout();
        this.router.navigateByUrl('/admin/login');
      });
  }

  private leerTitulo(): string {
    let ruta = this.router.routerState.snapshot.root;
    while (ruta.firstChild) ruta = ruta.firstChild;
    return ruta.data['titulo'] ?? '';
  }
}
