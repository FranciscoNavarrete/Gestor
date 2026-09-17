import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

const CHEQUEO_PERIODICO_MS = 6 * 60 * 60 * 1000;

@Injectable({ providedIn: 'root' })
export class ActualizacionService {
  private readonly swUpdate = inject(SwUpdate);
  private readonly snackBar = inject(MatSnackBar);

  escucharActualizaciones(): void {
    if (!this.swUpdate.isEnabled) return;

    this.swUpdate.versionUpdates
      .pipe(filter((evento): evento is VersionReadyEvent => evento.type === 'VERSION_READY'))
      .subscribe(() => this.avisarActualizacion());

    // La pestaña de un POS puede quedar abierta todo el día sin recargar — sin este chequeo
    // periódico, el service worker solo buscaría una versión nueva en el próximo reload completo.
    setInterval(() => this.swUpdate.checkForUpdate(), CHEQUEO_PERIODICO_MS);
  }

  private avisarActualizacion(): void {
    this.snackBar
      .open('Hay una versión nueva de la app', 'Actualizar')
      .onAction()
      .subscribe(() => document.location.reload());
  }
}
