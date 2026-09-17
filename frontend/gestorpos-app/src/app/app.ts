import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ActualizacionService } from './core/services/actualizacion.service';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  private readonly actualizacionService = inject(ActualizacionService);

  constructor() {
    this.actualizacionService.escucharActualizaciones();
  }
}
