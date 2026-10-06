import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CobrosNegocio, FluxoPlan } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';
import { extraerMensajeError } from '../../utils/error.util';

export interface CambiarPlanDialogData {
  tenantId: string;
  nombre: string;
}

/** Cambia el plan de un negocio con suscripción activa; rige desde el próximo cobro. Cierra con true si se cambió. */
@Component({
  selector: 'app-cambiar-plan-dialog',
  imports: [DatePipe, MatButtonModule, MatDialogModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './cambiar-plan-dialog.html',
  styleUrl: './cambiar-plan-dialog.scss',
})
export class CambiarPlanDialog implements OnInit {
  readonly data = inject<CambiarPlanDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<CambiarPlanDialog, boolean>);
  private readonly adminService = inject(AdminService);

  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly actual = signal<CobrosNegocio | null>(null);
  readonly planes = signal<FluxoPlan[]>([]);
  readonly elegidoId = signal<number | null>(null);

  // Los planes a los que se puede pasar: todos menos el que ya tiene.
  readonly opciones = computed(() => this.planes().filter((p) => p.mpPlanId !== this.actual()?.mpPlanId));
  readonly elegido = computed(() => this.planes().find((p) => p.mpPlanId === this.elegidoId()) ?? null);
  readonly hayPromo = computed(() => {
    const p = this.elegido();
    return !!p && !!p.montoPromo && !!p.mesesPromo;
  });
  // Lo que se cobra en el próximo cobro con el plan nuevo: el precio del mes 1 (con promo, el promocional).
  readonly montoProximo = computed(() => {
    const p = this.elegido();
    return p ? (this.hayPromo() ? p.montoPromo! : p.monto) : 0;
  });

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.adminService.obtenerCobros(this.data.tenantId).subscribe({
      next: (cobros) => {
        this.actual.set(cobros);
        this.adminService.listarPlanesFluxo().subscribe({
          next: (planes) => {
            this.planes.set(planes);
            this.cargando.set(false);
          },
          error: (err) => this.fallar(err),
        });
      },
      error: (err) => this.fallar(err),
    });
  }

  private fallar(err: unknown): void {
    this.error.set(extraerMensajeError(err));
    this.cargando.set(false);
  }

  dinero(valor: number): string {
    return '$' + Math.round(valor).toLocaleString('es-AR');
  }

  etiqueta(plan: FluxoPlan): string {
    return plan.montoPromo && plan.mesesPromo
      ? `${plan.nombre} · ${this.dinero(plan.montoPromo)} x ${plan.mesesPromo} meses, luego ${this.dinero(plan.monto)}`
      : `${plan.nombre} · ${this.dinero(plan.monto)} por mes`;
  }

  cambiar(): void {
    const plan = this.elegido();
    if (!plan || this.guardando()) return;
    this.guardando.set(true);
    this.error.set(null);
    this.adminService.cambiarPlan(this.data.tenantId, plan.mpPlanId).subscribe({
      next: () => this.dialogRef.close(true),
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }
}
