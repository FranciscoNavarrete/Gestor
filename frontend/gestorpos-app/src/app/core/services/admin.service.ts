import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AdminUsuario,
  CrearAdminUsuarioRequest,
  CrearNegocioRequest,
  FluxoPlan,
  LinkPago,
  TenantFeature,
  TenantResumen,
} from '../models/admin.models';
import { AdminAuthService } from './admin-auth.service';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http      = inject(HttpClient);
  private readonly adminAuth = inject(AdminAuthService);

  crearNegocio(request: CrearNegocioRequest): Observable<TenantResumen> {
    return this.http.post<TenantResumen>(`${environment.apiUrl}/admin/tenants`, request, {
      headers: this.headers(),
    });
  }

  obtenerLinkPago(tenantId: string): Observable<LinkPago> {
    return this.http.get<LinkPago>(`${environment.apiUrl}/admin/tenants/${tenantId}/link-pago`, {
      headers: this.headers(),
    });
  }

  listarPlanesFluxo(): Observable<FluxoPlan[]> {
    return this.http.get<FluxoPlan[]>(`${environment.apiUrl}/admin/fluxo/planes`, {
      headers: this.headers(),
    });
  }

  listarNegocios(): Observable<TenantResumen[]> {
    return this.http.get<TenantResumen[]>(`${environment.apiUrl}/admin/tenants`, {
      headers: this.headers(),
    });
  }

  desactivarNegocio(tenantId: string): Observable<TenantResumen> {
    return this.http.post<TenantResumen>(
      `${environment.apiUrl}/admin/tenants/${tenantId}/desactivar`,
      {},
      { headers: this.headers() },
    );
  }

  activarNegocio(tenantId: string): Observable<TenantResumen> {
    return this.http.post<TenantResumen>(
      `${environment.apiUrl}/admin/tenants/${tenantId}/activar`,
      {},
      { headers: this.headers() },
    );
  }

  listarFeatures(tenantId: string): Observable<TenantFeature[]> {
    return this.http.get<TenantFeature[]>(`${environment.apiUrl}/admin/tenants/${tenantId}/features`, {
      headers: this.headers(),
    });
  }

  activarFeature(tenantId: string, clave: string): Observable<TenantFeature> {
    return this.http.put<TenantFeature>(
      `${environment.apiUrl}/admin/tenants/${tenantId}/features/${clave}`,
      {},
      { headers: this.headers() },
    );
  }

  desactivarFeature(tenantId: string, clave: string): Observable<TenantFeature> {
    return this.http.delete<TenantFeature>(`${environment.apiUrl}/admin/tenants/${tenantId}/features/${clave}`, {
      headers: this.headers(),
    });
  }

  listarUsuarios(): Observable<AdminUsuario[]> {
    return this.http.get<AdminUsuario[]>(`${environment.apiUrl}/admin/usuarios`, { headers: this.headers() });
  }

  crearUsuario(request: CrearAdminUsuarioRequest): Observable<AdminUsuario> {
    return this.http.post<AdminUsuario>(`${environment.apiUrl}/admin/usuarios`, request, { headers: this.headers() });
  }

  activarUsuario(id: string): Observable<AdminUsuario> {
    return this.http.post<AdminUsuario>(`${environment.apiUrl}/admin/usuarios/${id}/activar`, {}, { headers: this.headers() });
  }

  desactivarUsuario(id: string): Observable<AdminUsuario> {
    return this.http.post<AdminUsuario>(`${environment.apiUrl}/admin/usuarios/${id}/desactivar`, {}, { headers: this.headers() });
  }

  private headers(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.adminAuth.token() ?? ''}` });
  }
}
