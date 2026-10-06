import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EfectivoMovimiento } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';
import { extraerMensajeError } from '../../utils/error.util';

export interface MovimientosEfectivoDialogData {
  vendedorId: string;
  nombre: string;
}

const ETIQUETAS: Record<EfectivoMovimiento['tipo'], string> = {
  Cobro: 'Cobró',
  Entrega: 'Entregó',
  Compensacion: 'Descontado',
};

/** Cobros, entregas y descuentos de efectivo de un vendedor. Cierra con true si se anuló una entrega. */
@Component({
  selector: 'app-movimientos-efectivo-dialog',
  imports: [DatePipe, MatButtonModule, MatDialogModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './movimientos-efectivo-dialog.html',
  styleUrl: './movimientos-efectivo-dialog.scss',
})
export class MovimientosEfectivoDialog implements OnInit {
  readonly data = inject<MovimientosEfectivoDialogData>(MAT_DIALOG_DATA);
  private readonly adminService = inject(AdminService);

  readonly cargando = signal(true);
  readonly error = signal<string | null>(null);
  readonly movimientos = signal<EfectivoMovimiento[]>([]);
  readonly cambio = signal(false);
  readonly confirmando = signal<string | null>(null);

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.adminService.movimientosEfectivo(this.data.vendedorId).subscribe({
      next: (lista) => {
        this.movimientos.set(lista);
        this.cargando.set(false);
      },
      error: (err) => {
        this.error.set(extraerMensajeError(err));
        this.cargando.set(false);
      },
    });
  }

  anular(m: EfectivoMovimiento): void {
    if (this.confirmando() !== m.id) {
      this.confirmando.set(m.id);
      return;
    }
    this.adminService.anularEntrega(m.id).subscribe({
      next: () => {
        this.cambio.set(true);
        this.confirmando.set(null);
        this.cargar();
      },
      error: (err) => this.error.set(extraerMensajeError(err)),
    });
  }

  etiqueta(tipo: EfectivoMovimiento['tipo']): string {
    return ETIQUETAS[tipo];
  }

  dinero(valor: number): string {
    return '$' + Math.round(valor).toLocaleString('es-AR');
  }
}
