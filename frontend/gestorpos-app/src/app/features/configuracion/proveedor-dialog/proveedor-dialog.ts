import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Proveedor } from '../../../core/models/compra.models';
import { CompraService } from '../../../core/services/compra.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

export interface ProveedorDialogData {
  proveedor: Proveedor | null;
}

@Component({
  selector: 'app-proveedor-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  templateUrl: './proveedor-dialog.html',
  styleUrl: './proveedor-dialog.scss',
})
export class ProveedorDialog {
  private readonly fb = inject(FormBuilder);
  private readonly compraService = inject(CompraService);
  private readonly dialogRef = inject(MatDialogRef<ProveedorDialog>);
  private readonly data = inject<ProveedorDialogData | null>(MAT_DIALOG_DATA, { optional: true });

  readonly esEdicion = !!this.data?.proveedor;
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    nombre: [this.data?.proveedor?.nombre ?? '', [Validators.required]],
    cuit: [this.data?.proveedor?.cuit ?? ''],
    telefono: [this.data?.proveedor?.telefono ?? ''],
    email: [this.data?.proveedor?.email ?? ''],
  });

  guardar(): void {
    if (this.form.invalid || this.guardando()) return;

    this.guardando.set(true);
    this.error.set(null);

    const valores = this.form.getRawValue();
    const request = {
      nombre: valores.nombre,
      cuit: valores.cuit.trim() || null,
      telefono: valores.telefono.trim() || null,
      email: valores.email.trim() || null,
    };
    const request$ = this.esEdicion
      ? this.compraService.editarProveedor(this.data!.proveedor!.id, request)
      : this.compraService.crearProveedor(request);

    request$.subscribe({
      next: (proveedor: Proveedor) => this.dialogRef.close(proveedor),
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }
}
