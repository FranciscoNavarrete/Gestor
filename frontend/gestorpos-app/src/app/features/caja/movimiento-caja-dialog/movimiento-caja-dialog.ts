import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CajaDto } from '../../../core/models/reportes.models';
import { CajaService } from '../../../core/services/caja.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

@Component({
  selector: 'app-movimiento-caja-dialog',
  imports: [FormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule],
  templateUrl: './movimiento-caja-dialog.html',
  styleUrl: './movimiento-caja-dialog.scss',
})
export class MovimientoCajaDialog {
  private readonly cajaService = inject(CajaService);
  private readonly dialogRef = inject(MatDialogRef<MovimientoCajaDialog>);

  readonly tipo = signal<'Ingreso' | 'Egreso'>('Egreso');
  readonly monto = signal<number | null>(null);
  readonly motivo = signal('');
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly invalido = computed(() => {
    const monto = this.monto();
    return monto == null || monto <= 0 || this.motivo().trim().length === 0;
  });

  guardar(): void {
    const monto = this.monto();
    if (this.guardando() || this.invalido() || monto == null) return;

    this.guardando.set(true);
    this.error.set(null);

    this.cajaService.agregarMovimiento({ tipo: this.tipo(), monto, motivo: this.motivo().trim() }).subscribe({
      next: (caja: CajaDto) => this.dialogRef.close(caja),
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }
}
