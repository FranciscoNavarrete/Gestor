import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.estaAutenticado()) {
    // El dueño que todavía no aceptó los términos no entra al sistema hasta aceptarlos.
    if (authService.terminosPendientes()) return router.createUrlTree(['/terminos']);
    return true;
  }

  router.navigate(['/login']);
  return false;
};

/** Solo exige estar logueado (sin el chequeo de términos): para la propia pantalla de aceptación. */
export const sesionGuard: CanActivateFn = () => {
  const router = inject(Router);
  if (inject(AuthService).estaAutenticado()) return true;
  return router.createUrlTree(['/login']);
};
