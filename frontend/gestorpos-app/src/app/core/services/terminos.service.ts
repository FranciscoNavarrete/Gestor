import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Terminos } from '../models/terminos.models';

/** Términos y condiciones vigentes y su aceptación por el dueño del negocio. */
@Injectable({ providedIn: 'root' })
export class TerminosService {
  private readonly http = inject(HttpClient);

  vigentes(): Observable<Terminos> {
    return this.http.get<Terminos>(`${environment.apiUrl}/terminos`);
  }

  aceptar(): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/terminos/aceptar`, {});
  }
}
