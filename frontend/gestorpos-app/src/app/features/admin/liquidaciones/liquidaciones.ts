import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LiquidacionDialog, ModoLiquidacion } from '../../../core/dialogs/liquidacion-dialog/liquidacion-dialog';
import { Liquidacion, LiquidacionVendedor, LiquidacionesMes } from '../../../core/models/admin.models';
import { AdminAuthService } from '../../../core/services/admin-auth.service';
import { AdminService } from '../../../core/services/admin.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

const MESES = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];

interface MesElegible {
  anio: number;
  mes: number;
  etiqueta: string;
}

@Component({
  selector: 'app-liquidaciones-admin',
  imports: [DatePipe, MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './liquidaciones.html',
  styleUrl: './liquidaciones.scss',
})
export class LiquidacionesAdmin implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly dialog = inject(MatDialog);
  protected readonly adminAuth = inject(AdminAuthService);

  // Solo meses terminados: los últimos 6, del más nuevo al más viejo.
  readonly meses: MesElegible[] = this.armarMeses();
  readonly mesElegido = signal<MesElegible>(this.meses[0]);

  readonly cargando = signal(false);
  readonly error = signal<string | null>(null);
  readonly cierre = signal<LiquidacionesMes | null>(null);
  readonly misPagos = signal<Liquidacion[]>([]);
  readonly expandida = signal<string | null>(null);

  ngOnInit(): void {
    this.cargar();
  }

  private armarMeses(): MesElegible[] {
    const hoy = new Date();
    return Array.from({ length: 6 }, (_, i) => {
      const d = new Date(hoy.getFullYear(), hoy.getMonth() - 1 - i, 1);
      return { anio: d.getFullYear(), mes: d.getMonth() + 1, etiqueta: `${MESES[d.getMonth()]} ${d.getFullYear()}` };
    });
  }

  elegirMes(mes: MesElegible): void {
    this.mesElegido.set(mes);
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);

    if (this.adminAuth.esOperador()) {
      const { anio, mes } = this.mesElegido();
      this.adminService.obtenerLiquidacionesMes(anio, mes).subscribe({
        next: (cierre) => {
          this.cierre.set(cierre);
          this.cargando.set(false);
        },
        error: (err) => {
          this.cargando.set(false);
          this.cierre.set(null);
          this.error.set(extraerMensajeError(err));
        },
      });
      return;
    }

    this.adminService.listarLiquidaciones().subscribe({
      next: (lista) => {
        this.misPagos.set(lista);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }

  abrir(modo: ModoLiquidacion, vendedor: LiquidacionVendedor, liquidacion?: Liquidacion): void {
    const { anio, mes } = this.mesElegido();
    this.dialog
      .open(LiquidacionDialog, {
        data: { modo, vendedorId: vendedor.vendedorId, vendedorNombre: vendedor.nombre, anio, mes, liquidacion, esOperador: true },
        width: '460px',
        maxWidth: '94vw',
      })
      .afterClosed()
      .subscribe((cambio) => {
        if (cambio) this.cargar();
      });
  }

  alternar(id: string): void {
    this.expandida.set(this.expandida() === id ? null : id);
  }

  dinero(valor: number): string {
    return '$' + Math.round(valor).toLocaleString('es-AR');
  }

  nombreMes(mes: number): string {
    return MESES[mes - 1];
  }

  estadoGeneral(v: LiquidacionVendedor): 'sin-liquidar' | 'pendiente' | 'pagada' {
    if (v.ventasSinLiquidar > 0 && v.liquidaciones.length === 0) return 'sin-liquidar';
    if (v.liquidaciones.some((l) => l.estado === 'Pendiente') || v.ventasSinLiquidar > 0) return 'pendiente';
    return 'pagada';
  }
}
