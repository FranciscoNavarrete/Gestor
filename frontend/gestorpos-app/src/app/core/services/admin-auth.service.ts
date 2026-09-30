import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

const STORAGE_KEY = 'gestorpos_admin_token';

export interface AdminLoginResponse {
  token: string;
  expiraUtc: string;
  nombre: string;
  rol: 'Operador' | 'Vendedor';
}

@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  private readonly http = inject(HttpClient);
  private readonly _token = signal<string | null>(localStorage.getItem(STORAGE_KEY));

  readonly token       = this._token.asReadonly();
  readonly estaLogueado = computed(() => !!this._token());

  login(email: string, password: string): Observable<AdminLoginResponse> {
    return this.http
      .post<AdminLoginResponse>(`${environment.apiUrl}/admin/auth/login`, { email, password })
      .pipe(tap(res => this._guardarToken(res.token)));
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this._token.set(null);
  }

  private _guardarToken(token: string): void {
    localStorage.setItem(STORAGE_KEY, token);
    this._token.set(token);
  }
}
