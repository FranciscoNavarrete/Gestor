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
  readonly debeCambiarPassword = computed(() => this.authState()?.debeCambiarPassword === true);
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

  /** Cambia la contraseña del usuario logueado (también sirve para salir de una temporal). */
  cambiarPassword(passwordActual: string, passwordNueva: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/cambiar-password`, { passwordActual, passwordNueva }).pipe(
      tap(() => {
        const actual = this.authState();
        if (actual) this.guardarSesion({ ...actual, debeCambiarPassword: false });
      }),
    );
  }

  private ultimoRefrescoDeFunciones = 0;

  /** Pregunta al servidor qué funciones tiene hoy el negocio y las actualiza en la sesión: así un cambio del
   * operador (por ejemplo prender «Medidas y presentaciones») llega sin cerrar sesión. Se limita a una consulta
   * cada 15 segundos y, si falla, se queda con lo que ya había. */
  refrescarFunciones(): Observable<void> {
    const actual = this.authState();
    if (!actual || Date.now() - this.ultimoRefrescoDeFunciones < 15_000) return of(undefined);
    this.ultimoRefrescoDeFunciones = Date.now();

    return this.http.get<{ features: string[] }>(`${environment.apiUrl}/auth/funciones`).pipe(
      tap(({ features }) => {
        const vigente = this.authState();
        if (!vigente) return;
        const antes = [...(vigente.features ?? [])].sort().join('|');
        const ahora = [...features].sort().join('|');
        if (antes !== ahora) this.guardarSesion({ ...vigente, features });
      }),
      map(() => undefined),
      catchError(() => of(undefined)),
    );
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
