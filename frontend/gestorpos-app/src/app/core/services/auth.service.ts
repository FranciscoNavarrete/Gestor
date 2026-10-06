import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
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
  readonly terminosPendientes = computed(() => this.authState()?.terminosPendientes === true);
  private readonly features = computed(() => new Set(this.authState()?.features ?? []));

  constructor(private readonly http: HttpClient) {}

  tieneFeature(clave: string): boolean {
    return this.features().has(clave);
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/login`, request)
      .pipe(tap((auth) => this.guardarSesion(auth)));
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.authState.set(null);
  }

  actualizarNombreNegocio(nombre: string): void {
    const actual = this.authState();
    if (!actual) return;
    this.guardarSesion({ ...actual, nombreNegocio: nombre });
  }

  /** Las sesiones iniciadas antes de que existieran los términos no traen el dato: se le pregunta al servidor una vez.
   * Devuelve true si el dueño todavía tiene que aceptarlos. Si no se puede saber, no bloquea. */
  verificarTerminos(): Observable<boolean> {
    const actual = this.authState();
    if (!actual || actual.rol !== 'Admin' || actual.terminosPendientes !== undefined) return of(actual?.terminosPendientes === true);
    return this.http.get<{ aceptados: boolean }>(`${environment.apiUrl}/terminos/estado`).pipe(
      tap((estado) => this.guardarSesion({ ...actual, terminosPendientes: !estado.aceptados })),
      map((estado) => !estado.aceptados),
      catchError(() => of(false)),
    );
  }

  marcarTerminosAceptados(): void {
    const actual = this.authState();
    if (!actual) return;
    this.guardarSesion({ ...actual, terminosPendientes: false });
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
