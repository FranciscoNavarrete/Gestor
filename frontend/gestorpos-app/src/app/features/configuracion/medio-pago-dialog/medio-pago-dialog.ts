import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MedioPagoDto } from '../../../core/models/configuracion.models';
import { ConfiguracionService } from '../../../core/services/configuracion.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

export interface MedioPagoDialogData {
  medioPago: MedioPagoDto | null;
}

@Component({
  selector: 'app-medio-pago-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  templateUrl: './medio-pago-dialog.html',
  styleUrl: './medio-pago-dialog.scss',
})
export class MedioPagoDialog {
  private readonly fb = inject(FormBuilder);
  private readonly configuracionService = inject(ConfiguracionService);
  private readonly dialogRef = inject(MatDialogRef<MedioPagoDialog>);
  private readonly data = inject<MedioPagoDialogData>(MAT_DIALOG_DATA);

  readonly esEdicion = !!this.data.medioPago;
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    nombre: [this.data.medioPago?.nombre ?? '', [Validators.required]],
  });

  guardar(): void {
    if (this.form.invalid || this.guardando()) return;

    this.guardando.set(true);
    this.error.set(null);

    const valores = this.form.getRawValue();
    const request$ = this.esEdicion
      ? this.configuracionService.editarMedioPago(this.data.medioPago!.id, valores)
      : this.configuracionService.crearMedioPago(valores);

    request$.subscribe({
      next: (medioPago: MedioPagoDto) => this.dialogRef.close(medioPago),
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }
}
