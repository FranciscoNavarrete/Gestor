import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ClienteDetalle } from '../../../../core/models/cliente.models';
import { MedioPagoDto } from '../../../../core/models/configuracion.models';
import { ClienteService } from '../../../../core/services/cliente.service';
import { ConfiguracionService } from '../../../../core/services/configuracion.service';
import { extraerMensajeError } from '../../../../core/utils/error.util';

export interface RegistrarPagoDialogData {
  clienteId: string;
  nombreCliente: string;
  saldo: number;
}

@Component({
  selector: 'app-registrar-pago-dialog',
  imports: [
    FormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './registrar-pago-dialog.html',
  styleUrl: './registrar-pago-dialog.scss',
})
export class RegistrarPagoDialog {
  private readonly clienteService = inject(ClienteService);
  private readonly configuracionService = inject(ConfiguracionService);
  private readonly dialogRef = inject(MatDialogRef<RegistrarPagoDialog>);
  readonly data = inject<RegistrarPagoDialogData>(MAT_DIALOG_DATA);

  readonly mediosPago = signal<MedioPagoDto[]>([]);
  readonly monto = signal<number | null>(null);
  readonly medioPago = signal('Efectivo');
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly quedaDebiendo = computed(() => {
    const monto = this.monto();
    return monto == null ? this.data.saldo : this.data.saldo - monto;
  });

  readonly montoInvalido = computed(() => {
    const monto = this.monto();
    return monto == null || monto <= 0 || monto > this.data.saldo;
  });

  constructor() {
    this.configuracionService
      .listarMediosPago()
      .subscribe((medios) => this.mediosPago.set(medios.filter((m) => m.activo)));
  }

  pagarTodo(): void {
    this.monto.set(this.data.saldo);
  }

  guardar(): void {
    const monto = this.monto();
    if (this.guardando() || this.montoInvalido() || monto == null) return;

    this.guardando.set(true);
    this.error.set(null);

    this.clienteService.registrarPagoCuenta(this.data.clienteId, { monto, medioPago: this.medioPago() }).subscribe({
      next: (cliente: ClienteDetalle) => this.dialogRef.close(cliente),
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }
}
