import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AbrirCajaRequest, CajaDto, CerrarCajaRequest } from '../models/reportes.models';

@Injectable({ providedIn: 'root' })
export class CajaService {
  constructor(private readonly http: HttpClient) {}

  actual(): Observable<CajaDto | null> {
    return this.http.get<CajaDto | null>(`${environment.apiUrl}/caja/actual`);
  }

  abrir(request: AbrirCajaRequest): Observable<CajaDto> {
    return this.http.post<CajaDto>(`${environment.apiUrl}/caja/abrir`, request);
  }

  cerrar(request: CerrarCajaRequest): Observable<CajaDto> {
    return this.http.post<CajaDto>(`${environment.apiUrl}/caja/cerrar`, request);
  }
}
