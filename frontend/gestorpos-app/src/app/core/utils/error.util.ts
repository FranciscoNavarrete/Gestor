import { HttpErrorResponse } from '@angular/common/http';

export function extraerMensajeError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const backendError = error.error as { error?: string } | null;
    if (backendError?.error) return backendError.error;
  }
  return 'Ocurrió un error inesperado. Intentá de nuevo.';
}
