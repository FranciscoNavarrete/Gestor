import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CajaDto } from '../../core/models/reportes.models';
import { CajaService } from '../../core/services/caja.service';
import { extraerMensajeError } from '../../core/utils/error.util';

@Component({
  selector: 'app-caja',
  imports: [FormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule],
  templateUrl: './caja.html',
  styleUrl: './caja.scss',
})
export class Caja implements OnInit {
  private readonly cajaService = inject(CajaService);

  readonly cargando = signal(true);
  readonly cajaActual = signal<CajaDto | null>(null);
  readonly cajaRecienCerrada = signal<CajaDto | null>(null);
  readonly procesando = signal(false);
  readonly error = signal<string | null>(null);

  readonly montoApertura = signal<number>(0);
  readonly montoCierreReal = signal<number>(0);

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.cajaService.actual().subscribe({
      next: (caja) => {
        this.cajaActual.set(caja);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }

  abrir(): void {
    if (this.procesando()) return;
    this.procesando.set(true);
    this.error.set(null);

    this.cajaService.abrir({ montoApertura: this.montoApertura() }).subscribe({
      next: (caja) => {
        this.procesando.set(false);
        this.cajaActual.set(caja);
        this.montoApertura.set(0);
      },
      error: (err) => {
        this.procesando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }

  cerrar(): void {
    if (this.procesando()) return;
    this.procesando.set(true);
    this.error.set(null);

    this.cajaService.cerrar({ montoCierreReal: this.montoCierreReal() }).subscribe({
      next: (caja) => {
        this.procesando.set(false);
        this.cajaActual.set(null);
        this.cajaRecienCerrada.set(caja);
        this.montoCierreReal.set(0);
      },
      error: (err) => {
        this.procesando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }

  volverAAbrir(): void {
    this.cajaRecienCerrada.set(null);
  }
}
