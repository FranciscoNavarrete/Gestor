import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CajaDto } from '../../core/models/reportes.models';
import { CajaService } from '../../core/services/caja.service';
import { extraerMensajeError } from '../../core/utils/error.util';
import { MovimientoCajaDialog } from './movimiento-caja-dialog/movimiento-caja-dialog';

@Component({
  selector: 'app-caja',
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './caja.html',
  styleUrl: './caja.scss',
})
export class Caja implements OnInit {
  private readonly cajaService = inject(CajaService);
  private readonly dialog = inject(MatDialog);

  readonly cargando = signal(true);
  readonly cajaActual = signal<CajaDto | null>(null);
  readonly cajaRecienCerrada = signal<CajaDto | null>(null);
  readonly procesando = signal(false);
  readonly error = signal<string | null>(null);

  readonly montoApertura = signal<number | null>(null);
  readonly montoCierreReal = signal<number | null>(null);

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
    const montoApertura = this.montoApertura();
    if (this.procesando() || montoApertura === null) return;
    this.procesando.set(true);
    this.error.set(null);

    this.cajaService.abrir({ montoApertura }).subscribe({
      next: (caja) => {
        this.procesando.set(false);
        this.cajaActual.set(caja);
        this.montoApertura.set(null);
      },
      error: (err) => {
        this.procesando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }

  cerrar(): void {
    const montoCierreReal = this.montoCierreReal();
    if (this.procesando() || montoCierreReal === null) return;
    this.procesando.set(true);
    this.error.set(null);

    this.cajaService.cerrar({ montoCierreReal }).subscribe({
      next: (caja) => {
        this.procesando.set(false);
        this.cajaActual.set(null);
        this.cajaRecienCerrada.set(caja);
        this.montoCierreReal.set(null);
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

  abrirMovimiento(): void {
    this.dialog
      .open(MovimientoCajaDialog, { width: '360px' })
      .afterClosed()
      .subscribe((caja: CajaDto | undefined) => {
        if (caja) this.cajaActual.set(caja);
      });
  }
}
