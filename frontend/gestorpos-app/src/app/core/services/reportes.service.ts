import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardDto } from '../models/reportes.models';

@Injectable({ providedIn: 'root' })
export class ReportesService {
  constructor(private readonly http: HttpClient) {}

  dashboard(): Observable<DashboardDto> {
    return this.http.get<DashboardDto>(`${environment.apiUrl}/reportes/dashboard`);
  }
}
