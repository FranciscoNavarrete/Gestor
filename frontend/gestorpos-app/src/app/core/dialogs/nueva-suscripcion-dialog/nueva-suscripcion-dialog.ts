import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FluxoPlan, TenantResumen } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';
import { MpService } from '../../services/mp.service';
import { extraerMensajeError } from '../../utils/error.util';

export interface NuevaSuscripcionDialogData {
  tenantId: string;
  nombre: string;
  /** Email del dueño: con el que se cobra la suscripción salvo que se cambie. */
  emailAdmin: string | null;
}

/** Cierra con el negocio actualizado (fluxoInitPoint viene si quedó para pagar por link) o con null si se canceló. */
@Component({
  selector: 'app-nueva-suscripcion-dialog',
  imports: [FormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule],
  templateUrl: './nueva-suscripcion-dialog.html',
  styleUrl: './nueva-suscripcion-dialog.scss',
})
export class NuevaSuscripcionDialog implements OnInit, OnDestroy {
  readonly data = inject<NuevaSuscripcionDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<NuevaSuscripcionDialog, TenantResumen | null>);
  private readonly adminService = inject(AdminService);
  private readonly mp = inject(MpService);

  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly tarjetaActiva = signal(false);
  readonly error = signal<string | null>(null);
  readonly planes = signal<FluxoPlan[]>([]);

  readonly planId = signal<number | null>(null);
  readonly incluirAlta = signal(false);
  readonly metodo = signal<'link' | 'tarjeta'>('link');
  email = this.data.emailAdmin ?? '';

  readonly plan = computed(() => this.planes().find((p) => p.mpPlanId === this.planId()) ?? null);

  // Lo que se cobra en el primer cobro: con alta el plan define el primer cobro (alta + 1er mes); sin alta, el precio del mes 1.
  readonly montoPrimerCobro = computed(() => {
    const p = this.plan();
    if (!p) return 0;
    const mes1 = p.montoPromo && p.mesesPromo ? p.montoPromo : p.monto;
    return this.incluirAlta() ? (p.montoPrimerCobro ?? mes1) : mes1;
  });
  readonly tieneAlta = computed(() => this.plan()?.montoPrimerCobro != null);

  ngOnInit(): void {
    this.adminService.listarPlanesFluxo().subscribe({
      next: (planes) => {
        this.planes.set(planes);
        this.cargando.set(false);
      },
      error: (err) => {
        this.error.set(extraerMensajeError(err));
        this.cargando.set(false);
      },
    });
  }

  ngOnDestroy(): void {
    this.mp.unmountBrick();
  }

  dinero(valor: number): string {
    return '$' + Math.round(valor).toLocaleString('es-AR');
  }

  etiqueta(plan: FluxoPlan): string {
    return plan.montoPromo && plan.mesesPromo
      ? `${plan.nombre} · ${this.dinero(plan.montoPromo)} x ${plan.mesesPromo} meses, luego ${this.dinero(plan.monto)}`
      : `${plan.nombre} · ${this.dinero(plan.monto)} por mes`;
  }

  crear(): void {
    if (!this.plan()) {
      this.error.set('Elegí un plan.');
      return;
    }
    if (!this.email.trim()) {
      this.error.set('Ingresá el email con el que se cobra.');
      return;
    }
    this.error.set(null);
    if (this.metodo() === 'tarjeta') {
      void this.continuarConTarjeta();
      return;
    }
    this.guardando.set(true);
    this.enviar(null).catch(() => this.guardando.set(false));
  }

  private async continuarConTarjeta(): Promise<void> {
    this.tarjetaActiva.set(true);
    // El contenedor del formulario se muestra recién con el cambio de pantalla.
    await new Promise((r) => setTimeout(r));
    try {
      await this.mp.mountCardPaymentBrick({
        containerId: 'brick-nueva-suscripcion',
        amount: this.montoPrimerCobro(),
        emailPagador: this.email.trim(),
        submitLabel: 'Crear suscripción y cobrar',
        onSubmit: (datos) => this.enviar(datos.token),
        onError: () => this.error.set('Mercado Pago no pudo procesar los datos de la tarjeta. Revisalos y probá de nuevo.'),
      });
    } catch {
      this.error.set('No se pudo cargar el formulario de tarjeta. Revisá la conexión y probá de nuevo.');
      this.cambiarDatos();
    }
  }

  cambiarDatos(): void {
    this.mp.unmountBrick();
    this.tarjetaActiva.set(false);
  }

  // Devuelve una promesa porque el formulario de tarjeta la espera: si se rechaza deja corregir y reintentar.
  private enviar(cardToken: string | null): Promise<void> {
    return new Promise<void>((resolver, rechazar) => {
      this.adminService
        .nuevaSuscripcion(this.data.tenantId, {
          mpPlanId: this.planId()!,
          cardToken,
          incluirAlta: this.incluirAlta(),
          emailPagador: this.email.trim(),
        })
        .subscribe({
          next: (negocio) => {
            resolver();
            this.dialogRef.close(negocio);
          },
          error: (err) => {
            this.error.set(extraerMensajeError(err));
            rechazar();
          },
        });
    });
  }
}
