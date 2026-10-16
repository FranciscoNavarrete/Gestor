import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AdminAuthService } from '../services/admin-auth.service';

export const adminAuthGuard: CanActivateFn = () => {
  const adminAuth = inject(AdminAuthService);
  const router = inject(Router);

  if (!adminAuth.estaLogueado()) {
    router.navigate(['/admin/login']);
    return false;
  }

  // Con una contraseña temporal (la restableció el operador) lo primero es elegir una nueva.
  if (adminAuth.debeCambiarPassword()) return router.createUrlTree(['/admin/cambiar-password']);
  return true;
};

/** Solo exige estar logueado: para la pantalla de cambio obligatorio de contraseña. */
export const adminSesionGuard: CanActivateFn = () => {
  const router = inject(Router);
  if (inject(AdminAuthService).estaLogueado()) return true;
  return router.createUrlTree(['/admin/login']);
};

/** Pantallas solo del operador: si entra un vendedor (por URL) se lo manda a Negocios, sin mensaje. */
export const adminOperadorGuard: CanActivateFn = () => {
  const adminAuth = inject(AdminAuthService);
  const router = inject(Router);
  return adminAuth.esOperador() ? true : router.createUrlTree(['/admin/negocios']);
};
