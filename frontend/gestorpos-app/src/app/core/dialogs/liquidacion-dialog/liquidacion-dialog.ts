import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Liquidacion, LiquidacionItem, PrevisualizacionLiquidacion } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';
import { extraerMensajeError } from '../../utils/error.util';

export type ModoLiquidacion = 'liquidar' | 'pagar' | 'ver';

export interface LiquidacionDialogData {
  modo: ModoLiquidacion;
  vendedorId: string;
  vendedorNombre: string;
  anio: number;
  mes: number;
  /** Para pagar y ver. */
  liquidacion?: Liquidacion;
  esOperador: boolean;
}

const MESES = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];

const hoyIso = (): string => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
};

/** Cierra con true si cambió algo (para que la pantalla se actualice). */
@Component({
  selector: 'app-liquidacion-dialog',
  imports: [DatePipe, FormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule],
  templateUrl: './liquidacion-dialog.html',
  styleUrl: './liquidacion-dialog.scss',
})
export class LiquidacionDialog implements OnInit {
  readonly data = inject<LiquidacionDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<LiquidacionDialog, boolean>);
  private readonly adminService = inject(AdminService);

  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly previsualizacion = signal<PrevisualizacionLiquidacion | null>(null);
  readonly confirmandoAnular = signal(false);

  readonly hoy = hoyIso();
  fechaPago = hoyIso();
  nota = '';

  ngOnInit(): void {
    if (this.data.modo === 'liquidar') this.cargarPrevisualizacion();
  }

  get periodo(): string {
    return `${MESES[this.data.mes - 1]} ${this.data.anio}`;
  }

  get items(): LiquidacionItem[] {
    return this.data.modo === 'liquidar' ? (this.previsualizacion()?.items ?? []) : (this.data.liquidacion?.items ?? []);
  }

  get total(): number {
    return this.data.modo === 'liquidar' ? (this.previsualizacion()?.total ?? 0) : (this.data.liquidacion?.total ?? 0);
  }

  /** Efectivo que el vendedor tiene a su cargo y se descuenta de esta liquidación. */
  get efectivo(): number {
    return this.data.modo === 'liquidar'
      ? (this.previsualizacion()?.efectivoEnPoder ?? 0)
      : (this.data.liquidacion?.efectivoCompensado ?? 0);
  }

  /** Lo que se le paga al vendedor; si es negativo, es lo que tiene que entregar. */
  get neto(): number {
    return this.total - this.efectivo;
  }

  absoluto(valor: number): number {
    return Math.abs(valor);
  }

  cargarPrevisualizacion(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.adminService.previsualizarLiquidacion(this.data.vendedorId, this.data.anio, this.data.mes).subscribe({
      next: (p) => {
        this.previsualizacion.set(p);
        this.cargando.set(false);
      },
      error: (err) => {
        this.error.set(extraerMensajeError(err));
        this.cargando.set(false);
      },
    });
  }

  liquidar(): void {
    this.ejecutar(this.adminService.liquidar(this.data.vendedorId, this.data.anio, this.data.mes));
  }

  pagar(): void {
    const id = this.data.liquidacion?.id;
    if (!id) return;
    this.ejecutar(this.adminService.pagarLiquidacion(id, this.fechaPago || null, this.nota.trim() || null));
  }

  anular(): void {
    const id = this.data.liquidacion?.id;
    if (!id) return;
    if (!this.confirmandoAnular()) {
      this.confirmandoAnular.set(true);
      return;
    }
    this.ejecutar(this.adminService.anularLiquidacion(id));
  }

  private ejecutar(accion: { subscribe: (o: { next: () => void; error: (e: unknown) => void }) => unknown }): void {
    this.guardando.set(true);
    this.error.set(null);
    accion.subscribe({
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

  etiquetaOrden(orden: number): string {
    return `${orden}ª venta del mes`;
  }
}
