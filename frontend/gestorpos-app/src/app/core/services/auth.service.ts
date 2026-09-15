import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest } from '../models/auth.models';

const STORAGE_KEY = 'gestorpos_auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authState = signal<AuthResponse | null>(this.leerDeStorage());

  readonly auth = this.authState.asReadonly();
  readonly estaAutenticado = computed(() => this.authState() !== null);
  readonly nombreNegocio = computed(() => this.authState()?.nombreNegocio ?? '');
  readonly nombreUsuario = computed(() => this.authState()?.nombreUsuario ?? '');
  readonly token = computed(() => this.authState()?.token ?? null);

  constructor(private readonly http: HttpClient) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/login`, request)
      .pipe(tap((auth) => this.guardarSesion(auth)));
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.authState.set(null);
  }

  private guardarSesion(auth: AuthResponse): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(auth));
    this.authState.set(auth);
  }

  private leerDeStorage(): AuthResponse | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthResponse;
    } catch {
      return null;
    }
  }
}
