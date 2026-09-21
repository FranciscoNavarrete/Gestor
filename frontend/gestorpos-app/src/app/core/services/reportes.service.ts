import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardDto, GananciasDto, RankingClienteDto, RankingProductoDto } from '../models/reportes.models';

@Injectable({ providedIn: 'root' })
export class ReportesService {
  constructor(private readonly http: HttpClient) {}

  dashboard(): Observable<DashboardDto> {
    return this.http.get<DashboardDto>(`${environment.apiUrl}/reportes/dashboard`);
  }

  rankingProductos(top: number): Observable<RankingProductoDto[]> {
    return this.http.get<RankingProductoDto[]>(`${environment.apiUrl}/reportes/ranking-productos`, {
      params: { top },
    });
  }

  rankingClientes(desde?: string, hasta?: string, top = 10): Observable<RankingClienteDto[]> {
    return this.http.get<RankingClienteDto[]>(`${environment.apiUrl}/reportes/ranking-clientes`, {
      params: { top, ...(desde && { desde }), ...(hasta && { hasta }) },
    });
  }

  ganancias(desde?: string, hasta?: string): Observable<GananciasDto> {
    return this.http.get<GananciasDto>(`${environment.apiUrl}/reportes/ganancias`, {
      params: { ...(desde && { desde }), ...(hasta && { hasta }) },
    });
  }

  exportarVentasPdf(desde?: string, hasta?: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/reportes/ventas/pdf`, {
      params: { ...(desde && { desde }), ...(hasta && { hasta }) },
      responseType: 'blob',
    });
  }

  exportarStockPdf(busqueda: string, soloBajoStock: boolean): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/reportes/stock/pdf`, {
      params: { busqueda, bajoStock: soloBajoStock },
      responseType: 'blob',
    });
  }
}
