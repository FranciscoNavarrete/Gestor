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
    // Panel interno del equipo de GestorPOS (Operador/Vendedor): un shell con menú y, adentro, una
    // pantalla por tarea. 'crear-negocio' era la pantalla única de antes y queda redirigida para no
    // romper la PWA ya instalada ni favoritos.
    path: 'admin',
    canActivate: [adminAuthGuard],
    loadComponent: () => import('./features/admin/admin-shell/admin-shell').then((m) => m.AdminShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'negocios' },
      { path: 'crear-negocio', pathMatch: 'full', redirectTo: 'negocios' },
      {
        path: 'negocios',
        data: { titulo: 'Negocios' },
        loadComponent: () => import('./features/admin/negocios/negocios').then((m) => m.Negocios),
      },
      {
        path: 'nuevo-negocio',
        data: { titulo: 'Nuevo negocio' },
        loadComponent: () =>
          import('./features/admin/nuevo-negocio/nuevo-negocio').then((m) => m.NuevoNegocio),
      },
      {
        // Vendedor: sus propias ventas y comisión. Operador: las de todos, con ranking.
        path: 'reportes',
        data: { titulo: 'Reportes' },
        loadComponent: () => import('./features/admin/reportes/reportes').then((m) => m.ReportesAdmin),
      },
      {
        // Solo Operador (lo exige el backend): quién hizo qué en el panel.
        path: 'movimientos',
        data: { titulo: 'Movimientos' },
        loadComponent: () =>
          import('./features/admin/movimientos/movimientos').then((m) => m.MovimientosAdminPantalla),
      },
      {
        // Solo Operador — gestión de usuarios admin (Operador/Vendedor).
        path: 'usuarios',
        data: { titulo: 'Usuarios' },
        loadComponent: () =>
          import('./features/admin/usuarios-admin/usuarios-admin').then((m) => m.UsuariosAdmin),
      },
    ],
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
