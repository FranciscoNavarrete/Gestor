import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MiSuscripcion } from '../models/suscripcion.models';

/** La suscripción del propio negocio (solo la ve su dueño). */
@Injectable({ providedIn: 'root' })
export class SuscripcionService {
  private readonly http = inject(HttpClient);

  obtener(): Observable<MiSuscripcion> {
    return this.http.get<MiSuscripcion>(`${environment.apiUrl}/suscripcion`);
  }

  cambiarTarjeta(cardToken: string): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/suscripcion/tarjeta`, { cardToken });
  }
}
