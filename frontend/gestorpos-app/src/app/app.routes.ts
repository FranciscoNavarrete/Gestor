import { Routes } from '@angular/router';
import { adminAuthGuard } from './core/guards/admin-auth.guard';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    // Ruta interna, no vinculada desde ningún lado de la UI pública: acá entra el equipo de
    // GestorPOS (Operador/Vendedor), no un negocio. Login propio, separado del de tenant.
    path: 'admin/login',
    loadComponent: () =>
      import('./features/admin/admin-login/admin-login').then((m) => m.AdminLogin),
  },
  {
    // El operador de GestorPOS la usa para dar de alta negocios de clientes.
    path: 'admin/crear-negocio',
    canActivate: [adminAuthGuard],
    loadComponent: () =>
      import('./features/admin/crear-negocio/crear-negocio').then((m) => m.CrearNegocio),
  },
  {
    // También interna, solo para Operador — gestión de usuarios admin (Operador/Vendedor).
    path: 'admin/usuarios',
    canActivate: [adminAuthGuard],
    loadComponent: () =>
      import('./features/admin/usuarios-admin/usuarios-admin').then((m) => m.UsuariosAdmin),
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
        path: 'compras',
        loadComponent: () => import('./features/compras/compras').then((m) => m.Compras),
      },
      {
        path: 'reportes',
        loadComponent: () => import('./features/reportes/reportes').then((m) => m.Reportes),
      },
      {
        path: 'configuracion',
        loadComponent: () => import('./features/configuracion/configuracion').then((m) => m.Configuracion),
      },
      {
        path: 'clientes',
        loadComponent: () => import('./features/clientes/clientes').then((m) => m.Clientes),
      },
      {
        path: 'clientes/:id',
        loadComponent: () =>
          import('./features/clientes/cliente-detalle/cliente-detalle').then((m) => m.ClienteDetalle),
      },
      {
        path: 'tareas',
        loadComponent: () => import('./features/tareas/tareas').then((m) => m.Tareas),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
