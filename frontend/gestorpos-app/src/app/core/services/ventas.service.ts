import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CrearVentaRequest, VentaDto, VentaResumenDto } from '../models/venta.models';

@Injectable({ providedIn: 'root' })
export class VentasService {
  constructor(private readonly http: HttpClient) {}

  crear(request: CrearVentaRequest): Observable<VentaDto> {
    return this.http.post<VentaDto>(`${environment.apiUrl}/ventas`, request);
  }

  obtener(id: string): Observable<VentaDto> {
    return this.http.get<VentaDto>(`${environment.apiUrl}/ventas/${id}`);
  }

  listar(desde?: string, hasta?: string): Observable<VentaResumenDto[]> {
    return this.http.get<VentaResumenDto[]>(`${environment.apiUrl}/ventas`, {
      params: { ...(desde && { desde }), ...(hasta && { hasta }) },
    });
  }
}
