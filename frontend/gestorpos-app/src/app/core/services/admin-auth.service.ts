import { Injectable, computed, signal } from '@angular/core';

const STORAGE_KEY = 'gestorpos_admin_key';

/// Guarda la API key del panel interno del operador (no confundir con el JWT de un negocio).
@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  private readonly keyState = signal<string | null>(localStorage.getItem(STORAGE_KEY));

  readonly apiKey = this.keyState.asReadonly();
  readonly tieneClave = computed(() => !!this.keyState());

  guardarClave(key: string): void {
    localStorage.setItem(STORAGE_KEY, key);
    this.keyState.set(key);
  }

  olvidarClave(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.keyState.set(null);
  }
}
