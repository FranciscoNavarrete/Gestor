import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminAuthService } from '../services/admin-auth.service';

// Separado del authInterceptor (que es para el JWT de tenant) porque AdminService arma su propio
// header Authorization a mano -- acá solo nos importa detectar un 401 en /api/admin y avisarle al
// usuario por qué lo mandamos de vuelta al login, en vez de desloguearlo en silencio.
export const adminAuthInterceptor: HttpInterceptorFn = (req, next) => {
  const adminAuth = inject(AdminAuthService);
  const snackBar  = inject(MatSnackBar);
  const router    = inject(Router);

  const esRequestAdmin = req.url.startsWith(`${environment.apiUrl}/admin`);

  return next(req).pipe(
    catchError((error) => {
      if (esRequestAdmin && error.status === 401 && adminAuth.estaLogueado()) {
        adminAuth.logout();
        snackBar.open('Tu sesión expiró, iniciá sesión de nuevo', 'OK', { duration: 5000 });
        router.navigate(['/admin/crear-negocio']);
      }
      return throwError(() => error);
    }),
  );
};
