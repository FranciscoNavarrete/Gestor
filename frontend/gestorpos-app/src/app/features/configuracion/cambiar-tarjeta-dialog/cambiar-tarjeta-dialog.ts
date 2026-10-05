import { AfterViewInit, Component, OnDestroy, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { SuscripcionService } from '../../../core/services/suscripcion.service';
import { MpService } from '../../../core/services/mp.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

export interface CambiarTarjetaDialogData {
  /** Lo que se va a cobrar la próxima vez: el formulario de tarjeta lo necesita. */
  monto: number;
  /** Email con el que se creó la suscripción: la tarjeta se tokeniza a nombre de ese email. */
  email: string;
}

/** Cierra con true si la tarjeta se cambió. */
@Component({
  selector: 'app-cambiar-tarjeta-dialog',
  imports: [MatButtonModule, MatDialogModule],
  templateUrl: './cambiar-tarjeta-dialog.html',
  styleUrl: './cambiar-tarjeta-dialog.scss',
})
export class CambiarTarjetaDialog implements AfterViewInit, OnDestroy {
  readonly data = inject<CambiarTarjetaDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<CambiarTarjetaDialog, boolean>);
  private readonly mp = inject(MpService);
  private readonly suscripcionService = inject(SuscripcionService);

  readonly error = signal<string | null>(null);
  readonly cargandoFormulario = signal(true);

  async ngAfterViewInit(): Promise<void> {
    try {
      await this.mp.mountCardPaymentBrick({
        containerId: 'brick-cambio-tarjeta',
        amount: this.data.monto,
        emailPagador: this.data.email,
        submitLabel: 'Guardar tarjeta',
        onSubmit: (datos) => this.guardar(datos.token),
        onError: () => this.error.set('Mercado Pago no pudo procesar los datos de la tarjeta. Revisalos y probá de nuevo.'),
      });
    } catch {
      this.error.set('No se pudo cargar el formulario de tarjeta. Revisá la conexión y probá de nuevo.');
    } finally {
      this.cargandoFormulario.set(false);
    }
  }

  ngOnDestroy(): void {
    this.mp.unmountBrick();
  }

  // El formulario espera una promesa: si se rechaza, deja que el usuario corrija y reintente.
  private guardar(token: string): Promise<void> {
    this.error.set(null);
    return new Promise<void>((resolver, rechazar) => {
      this.suscripcionService.cambiarTarjeta(token).subscribe({
        next: () => {
          resolver();
          this.dialogRef.close(true);
        },
        error: (err) => {
          this.error.set(extraerMensajeError(err));
          rechazar();
        },
      });
    });
  }

  dinero(valor: number): string {
    return '$' + Math.round(valor).toLocaleString('es-AR');
  }
}
