import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

const CHEQUEO_PERIODICO_MS = 30 * 60 * 1000;
const CHEQUEO_MINIMO_ENTRE_INTENTOS_MS = 5 * 60 * 1000;

@Injectable({ providedIn: 'root' })
export class ActualizacionService {
  private readonly swUpdate = inject(SwUpdate);
  private readonly snackBar = inject(MatSnackBar);

  private ultimoChequeo = 0;

  escucharActualizaciones(): void {
    if (!this.swUpdate.isEnabled) return;

    this.swUpdate.versionUpdates
      .pipe(filter((evento): evento is VersionReadyEvent => evento.type === 'VERSION_READY'))
      .subscribe(() => this.avisarActualizacion());

    // La pestaña de un POS puede quedar abierta todo el día sin recargar — sin chequear de vez en
    // cuando, el service worker solo buscaría una versión nueva en el próximo reload completo.
    setInterval(() => this.chequear(), CHEQUEO_PERIODICO_MS);

    // Además, cada vez que la pestaña vuelve a primer plano (el cajero la dejó de lado y volvió) —
    // es el momento natural en que alguien esperaría ver un cartel de "hay una versión nueva" si
    // se deployó algo mientras tanto, sin depender de que se cumplan los 30 minutos del intervalo.
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') this.chequear();
    });
  }

  private chequear(): void {
    const ahora = Date.now();
    if (ahora - this.ultimoChequeo < CHEQUEO_MINIMO_ENTRE_INTENTOS_MS) return;
    this.ultimoChequeo = ahora;
    this.swUpdate.checkForUpdate();
  }

  private avisarActualizacion(): void {
    this.snackBar
      .open('Hay una versión nueva de la app', 'Actualizar')
      .onAction()
      .subscribe(() => {
        // Sin activateUpdate(), un reload podía seguir sirviendo la versión vieja si el service
        // worker nuevo todavía no había tomado control de la página.
        this.swUpdate.activateUpdate().then(() => document.location.reload());
      });
  }
}
