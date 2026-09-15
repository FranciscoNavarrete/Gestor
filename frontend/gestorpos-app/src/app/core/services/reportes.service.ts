import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardDto, RankingProductoDto } from '../models/reportes.models';

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
}
