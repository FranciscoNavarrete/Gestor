import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.estaAutenticado()) {
    router.navigate(['/login']);
    return false;
  }

  // El dueño que todavía no aceptó los términos no entra al sistema hasta aceptarlos.
  return authService.verificarTerminos().pipe(
    map((pendientes) => (pendientes ? router.createUrlTree(['/terminos']) : true)),
  );
};

/** Solo exige estar logueado (sin el chequeo de términos): para la propia pantalla de aceptación. */
export const sesionGuard: CanActivateFn = () => {
  const router = inject(Router);
  if (inject(AuthService).estaAutenticado()) return true;
  return router.createUrlTree(['/login']);
};
