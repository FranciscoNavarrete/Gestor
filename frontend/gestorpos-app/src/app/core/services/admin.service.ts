import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CrearNegocioRequest, TenantResumen } from '../models/admin.models';
import { AdminAuthService } from './admin-auth.service';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly adminAuth = inject(AdminAuthService);

  crearNegocio(request: CrearNegocioRequest): Observable<TenantResumen> {
    return this.http.post<TenantResumen>(`${environment.apiUrl}/admin/tenants`, request, {
      headers: this.headers(),
    });
  }

  listarNegocios(): Observable<TenantResumen[]> {
    return this.http.get<TenantResumen[]>(`${environment.apiUrl}/admin/tenants`, {
      headers: this.headers(),
    });
  }

  private headers(): HttpHeaders {
    return new HttpHeaders({ 'X-Admin-Api-Key': this.adminAuth.apiKey() ?? '' });
  }
}
