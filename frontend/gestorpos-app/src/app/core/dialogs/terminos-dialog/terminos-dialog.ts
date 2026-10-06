import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Terminos } from '../../models/terminos.models';
import { TerminosService } from '../../services/terminos.service';

/** Los términos y condiciones completos, para leerlos desde el alta o desde cualquier otro lado. */
@Component({
  selector: 'app-terminos-dialog',
  imports: [MatButtonModule, MatDialogModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './terminos-dialog.html',
  styleUrl: './terminos-dialog.scss',
})
export class TerminosDialog implements OnInit {
  private readonly terminosService = inject(TerminosService);

  readonly cargando = signal(true);
  readonly error = signal(false);
  readonly terminos = signal<Terminos | null>(null);

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(false);
    this.terminosService.vigentes().subscribe({
      next: (t) => {
        this.terminos.set(t);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set(true);
        this.cargando.set(false);
      },
    });
  }
}
