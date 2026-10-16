import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

const STORAGE_KEY = 'gestorpos_admin_token';
const STORAGE_KEY_CAMBIAR = 'gestorpos_admin_debe_cambiar_password';

export interface AdminLoginResponse {
  token: string;
  expiraUtc: string;
  nombre: string;
  rol: 'Operador' | 'Vendedor';
  /** El operador le restableció la contraseña con una temporal: tiene que elegir una nueva antes de seguir. */
  debeCambiarPassword?: boolean;
}

interface ClaimsAdmin {
  admin_rol?: string;
  nombre?: string;
}

@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  private readonly http = inject(HttpClient);
  private readonly _token = signal<string | null>(localStorage.getItem(STORAGE_KEY));

  readonly token        = this._token.asReadonly();
  readonly estaLogueado = computed(() => !!this._token());
  private readonly _debeCambiar = signal(localStorage.getItem(STORAGE_KEY_CAMBIAR) === '1');
  readonly debeCambiarPassword = this._debeCambiar.asReadonly();

  private readonly claims = computed<ClaimsAdmin>(() => {
    const token = this._token();
    if (!token) return {};
    try {
      return JSON.parse(atob(token.split('.')[1]));
    } catch {
      return {};
    }
  });

  readonly rol        = computed(() => this.claims().admin_rol ?? '');
  readonly nombre     = computed(() => this.claims().nombre ?? '');
  readonly esOperador = computed(() => this.rol() === 'Operador');

  login(email: string, password: string): Observable<AdminLoginResponse> {
    return this.http
      .post<AdminLoginResponse>(`${environment.apiUrl}/admin/auth/login`, { email, password })
      .pipe(tap(res => {
        this._guardarToken(res.token);
        this._marcarDebeCambiar(res.debeCambiarPassword === true);
      }));
  }

  /** Cambia la contraseña del usuario logueado (también sirve para salir de una temporal). */
  cambiarPassword(passwordActual: string, passwordNueva: string): Observable<void> {
    return this.http
      .post<void>(`${environment.apiUrl}/admin/auth/cambiar-password`, { passwordActual, passwordNueva }, {
        headers: { Authorization: `Bearer ${this._token()}` },
      })
      .pipe(tap(() => this._marcarDebeCambiar(false)));
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this._token.set(null);
    this._marcarDebeCambiar(false);
  }

  private _marcarDebeCambiar(valor: boolean): void {
    if (valor) localStorage.setItem(STORAGE_KEY_CAMBIAR, '1');
    else localStorage.removeItem(STORAGE_KEY_CAMBIAR);
    this._debeCambiar.set(valor);
  }

  private _guardarToken(token: string): void {
    localStorage.setItem(STORAGE_KEY, token);
    this._token.set(token);
  }
}
