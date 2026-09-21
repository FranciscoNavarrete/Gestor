import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ClaveVapida, DesuscribirsePushRequest, SuscribirsePushRequest } from '../models/notificacion.models';

@Injectable({ providedIn: 'root' })
export class NotificacionPushService {
  constructor(
    private readonly http: HttpClient,
    private readonly swPush: SwPush,
  ) {}

  get soportado(): boolean {
    return this.swPush.isEnabled;
  }

  async estaSuscripto(): Promise<boolean> {
    if (!this.soportado) return false;
    const suscripcion = await firstValueFrom(this.swPush.subscription);
    return suscripcion !== null;
  }

  async suscribirse(): Promise<void> {
    if (!this.soportado) throw new Error('Las notificaciones push no están disponibles en este navegador.');

    const { clavePublica } = await firstValueFrom(
      this.http.get<ClaveVapida>(`${environment.apiUrl}/notificaciones/clave-publica`),
    );
    if (!clavePublica) throw new Error('El servidor no tiene configuradas las notificaciones push.');

    const suscripcion = await this.swPush.requestSubscription({ serverPublicKey: clavePublica });
    const json = suscripcion.toJSON();

    const request: SuscribirsePushRequest = {
      endpoint: json.endpoint!,
      p256dh: json.keys!['p256dh'],
      auth: json.keys!['auth'],
    };
    await firstValueFrom(this.http.post(`${environment.apiUrl}/notificaciones/suscribirse`, request));
  }

  async desuscribirse(): Promise<void> {
    if (!this.soportado) return;

    const suscripcion = await firstValueFrom(this.swPush.subscription);
    if (!suscripcion) return;

    const endpoint = suscripcion.endpoint;
    await suscripcion.unsubscribe();

    const request: DesuscribirsePushRequest = { endpoint };
    await firstValueFrom(this.http.post(`${environment.apiUrl}/notificaciones/desuscribirse`, request));
  }
}
