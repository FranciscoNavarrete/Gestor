import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AdminService } from '../../services/admin.service';
import { extraerMensajeError } from '../../utils/error.util';

export interface EntregaEfectivoDialogData {
  vendedorId: string;
  nombre: string;
  /** Lo que tiene hoy a su cargo: no puede entregar más que eso. */
  enPoder: number;
}

const hoyIso = (): string => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
};

/** Cierra con true si se registró la entrega. */
@Component({
  selector: 'app-entrega-efectivo-dialog',
  imports: [FormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  templateUrl: './entrega-efectivo-dialog.html',
  styleUrl: './entrega-efectivo-dialog.scss',
})
export class EntregaEfectivoDialog {
  readonly data = inject<EntregaEfectivoDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<EntregaEfectivoDialog, boolean>);
  private readonly adminService = inject(AdminService);

  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly hoy = hoyIso();
  monto: number | null = this.data.enPoder;
  fecha = hoyIso();
  nota = '';

  guardar(): void {
    if (!this.monto || this.monto <= 0) {
      this.error.set('Ingresá el monto que entregó.');
      return;
    }
    this.guardando.set(true);
    this.error.set(null);
    this.adminService.registrarEntrega(this.data.vendedorId, this.monto, this.fecha || null, this.nota.trim() || null).subscribe({
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
