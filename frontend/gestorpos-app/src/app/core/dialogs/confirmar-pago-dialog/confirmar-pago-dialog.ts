import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ConfirmarPagoRequest } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';
import { extraerMensajeError } from '../../utils/error.util';

export interface ConfirmarPagoDialogData {
  tenantId: string;
  nombre: string;
  /** Monto que se esperaba recibir. */
  monto: number;
  /** Cuándo cobra por primera vez la suscripción de tarjeta, si ya se sabe. */
  proximoCobro: string | null;
  /** La suscripción todavía no está autorizada (el cliente no abrió el link). */
  faltaLink: boolean;
}

const hoyIso = (): string => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
};

/** Cierra con true si se confirmó el pago. */
@Component({
  selector: 'app-confirmar-pago-dialog',
  imports: [DatePipe, FormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  templateUrl: './confirmar-pago-dialog.html',
  styleUrl: './confirmar-pago-dialog.scss',
})
export class ConfirmarPagoDialog {
  readonly data = inject<ConfirmarPagoDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ConfirmarPagoDialog, boolean>);
  private readonly adminService = inject(AdminService);

  readonly metodos: ConfirmarPagoRequest['metodo'][] = ['Transferencia', 'Tarjeta', 'Otro'];
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly hoy = hoyIso();
  metodo: ConfirmarPagoRequest['metodo'] = 'Transferencia';
  monto: number | null = this.data.monto;
  fecha = hoyIso();
  nota = '';

  confirmar(): void {
    if (!this.monto || this.monto <= 0) {
      this.error.set('Ingresá el monto recibido.');
      return;
    }
    this.guardando.set(true);
    this.error.set(null);
    this.adminService
      .confirmarPago(this.data.tenantId, {
        metodo: this.metodo,
        monto: this.monto,
        fechaRecepcion: this.fecha || null,
        nota: this.nota.trim() || null,
      })
      .subscribe({
        next: () => this.dialogRef.close(true),
        error: (err) => {
          this.guardando.set(false);
          this.error.set(extraerMensajeError(err));
        },
      });
  }

  dinero(valor: number): string {
    return '$' + Math.round(valor).toLocaleString('es-AR');
  }
}
