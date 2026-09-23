import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { FacturaProveedorDto } from '../../../core/models/factura-proveedor.models';
import { MedioPagoDto } from '../../../core/models/configuracion.models';
import { ConfiguracionService } from '../../../core/services/configuracion.service';
import { FacturaProveedorService } from '../../../core/services/factura-proveedor.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

export interface RegistrarPagoFacturaDialogData {
  factura: FacturaProveedorDto;
}

@Component({
  selector: 'app-registrar-pago-factura-dialog',
  imports: [
    FormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './registrar-pago-factura-dialog.html',
  styleUrl: './registrar-pago-factura-dialog.scss',
})
export class RegistrarPagoFacturaDialog {
  private readonly facturaProveedorService = inject(FacturaProveedorService);
  private readonly configuracionService = inject(ConfiguracionService);
  private readonly dialogRef = inject(MatDialogRef<RegistrarPagoFacturaDialog>);
  readonly data = inject<RegistrarPagoFacturaDialogData>(MAT_DIALOG_DATA);

  readonly mediosPago = signal<MedioPagoDto[]>([]);
  readonly monto = signal<number | null>(null);
  readonly medioPago = signal('Efectivo');
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly quedaPendiente = computed(() => {
    const monto = this.monto();
    return monto == null ? this.data.factura.saldo : this.data.factura.saldo - monto;
  });

  readonly montoInvalido = computed(() => {
    const monto = this.monto();
    return monto == null || monto <= 0 || monto > this.data.factura.saldo;
  });

  constructor() {
    this.configuracionService
      .listarMediosPago()
      .subscribe((medios) => this.mediosPago.set(medios.filter((m) => m.activo)));
  }

  pagarTodo(): void {
    this.monto.set(this.data.factura.saldo);
  }

  guardar(): void {
    const monto = this.monto();
    if (this.guardando() || this.montoInvalido() || monto == null) return;

    this.guardando.set(true);
    this.error.set(null);

    this.facturaProveedorService.registrarPago(this.data.factura.id, { monto, medioPago: this.medioPago() }).subscribe({
      next: (factura: FacturaProveedorDto) => this.dialogRef.close(factura),
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }
}
