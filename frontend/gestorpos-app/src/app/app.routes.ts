import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    // Ruta interna, no vinculada desde ningún lado de la UI pública: el operador de GestorPOS
    // la usa para dar de alta negocios de clientes. Gate propio por API key (no JWT de tenant).
    path: 'admin/crear-negocio',
    loadComponent: () =>
      import('./features/admin/crear-negocio/crear-negocio').then((m) => m.CrearNegocio),
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell').then((m) => m.Shell),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'venta',
        loadComponent: () => import('./features/venta/venta').then((m) => m.Venta),
      },
      {
        path: 'productos',
        loadComponent: () => import('./features/productos/productos').then((m) => m.Productos),
      },
      {
        path: 'caja',
        loadComponent: () => import('./features/caja/caja').then((m) => m.Caja),
      },
      {
        path: 'reportes',
        loadComponent: () => import('./features/reportes/reportes').then((m) => m.Reportes),
      },
      {
        path: 'configuracion',
        loadComponent: () => import('./features/configuracion/configuracion').then((m) => m.Configuracion),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
