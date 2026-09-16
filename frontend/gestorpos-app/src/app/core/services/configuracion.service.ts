import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ActualizarNegocioRequest,
  CrearMedioPagoRequest,
  EditarMedioPagoRequest,
  MedioPagoDto,
  NegocioDto,
} from '../models/configuracion.models';

@Injectable({ providedIn: 'root' })
export class ConfiguracionService {
  constructor(private readonly http: HttpClient) {}

  listarMediosPago(): Observable<MedioPagoDto[]> {
    return this.http.get<MedioPagoDto[]>(`${environment.apiUrl}/medios-pago`);
  }

  crearMedioPago(request: CrearMedioPagoRequest): Observable<MedioPagoDto> {
    return this.http.post<MedioPagoDto>(`${environment.apiUrl}/medios-pago`, request);
  }

  editarMedioPago(id: string, request: EditarMedioPagoRequest): Observable<MedioPagoDto> {
    return this.http.put<MedioPagoDto>(`${environment.apiUrl}/medios-pago/${id}`, request);
  }

  desactivarMedioPago(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/medios-pago/${id}`);
  }

  obtenerNegocio(): Observable<NegocioDto> {
    return this.http.get<NegocioDto>(`${environment.apiUrl}/negocio`);
  }

  actualizarNegocio(request: ActualizarNegocioRequest): Observable<NegocioDto> {
    return this.http.put<NegocioDto>(`${environment.apiUrl}/negocio`, request);
  }

  subirLogo(archivo: File): Observable<NegocioDto> {
    const formData = new FormData();
    formData.append('archivo', archivo);
    return this.http.post<NegocioDto>(`${environment.apiUrl}/negocio/logo`, formData);
  }

  quitarLogo(): Observable<NegocioDto> {
    return this.http.delete<NegocioDto>(`${environment.apiUrl}/negocio/logo`);
  }

  obtenerLogoBlob(): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/negocio/logo`, { responseType: 'blob' });
  }
}
