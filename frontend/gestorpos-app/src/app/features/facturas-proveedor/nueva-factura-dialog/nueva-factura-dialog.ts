import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Proveedor } from '../../../core/models/compra.models';
import { FacturaProveedorDto } from '../../../core/models/factura-proveedor.models';
import { FacturaProveedorService } from '../../../core/services/factura-proveedor.service';
import { extraerMensajeError } from '../../../core/utils/error.util';
import { fechaAIso, hoy } from '../../../core/utils/fecha.util';

export interface NuevaFacturaDialogData {
  proveedores: Proveedor[];
  proveedorIdPreseleccionado: string | null;
}

@Component({
  selector: 'app-nueva-factura-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './nueva-factura-dialog.html',
  styleUrl: './nueva-factura-dialog.scss',
})
export class NuevaFacturaDialog {
  private readonly fb = inject(FormBuilder);
  private readonly facturaProveedorService = inject(FacturaProveedorService);
  private readonly dialogRef = inject(MatDialogRef<NuevaFacturaDialog>);
  readonly data = inject<NuevaFacturaDialogData>(MAT_DIALOG_DATA);

  readonly hoy = hoy();
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly proveedoresActivos = this.data.proveedores.filter((p) => p.activo);

  readonly form = this.fb.nonNullable.group({
    proveedorId: [this.data.proveedorIdPreseleccionado ?? '', [Validators.required]],
    numeroFactura: ['', [Validators.required]],
    monto: [null as number | null, [Validators.required, Validators.min(0.01)]],
    fechaEmision: [this.hoy, [Validators.required]],
    fechaVencimiento: [null as Date | null],
  });

  guardar(): void {
    if (this.form.invalid || this.guardando()) return;

    this.guardando.set(true);
    this.error.set(null);

    const valores = this.form.getRawValue();
    this.facturaProveedorService
      .crear({
        proveedorId: valores.proveedorId,
        numeroFactura: valores.numeroFactura,
        monto: valores.monto!,
        fechaEmision: fechaAIso(valores.fechaEmision),
        fechaVencimiento: valores.fechaVencimiento ? fechaAIso(valores.fechaVencimiento) : null,
      })
      .subscribe({
        next: (factura: FacturaProveedorDto) => this.dialogRef.close(factura),
        error: (err) => {
          this.guardando.set(false);
          this.error.set(extraerMensajeError(err));
        },
      });
  }
}
