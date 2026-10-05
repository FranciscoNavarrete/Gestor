import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CobrosNegocio } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';
import { extraerMensajeError } from '../../utils/error.util';

export interface CobrosDialogData {
  tenantId: string;
  nombre: string;
}

const ETIQUETAS: Record<string, string> = {
  aprobado: 'Aprobado',
  rechazado: 'Rechazado',
  pendiente: 'En proceso',
  cancelado: 'Cancelado',
};

@Component({
  selector: 'app-cobros-dialog',
  imports: [DatePipe, MatButtonModule, MatDialogModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './cobros-dialog.html',
  styleUrl: './cobros-dialog.scss',
})
export class CobrosDialog implements OnInit {
  readonly data = inject<CobrosDialogData>(MAT_DIALOG_DATA);
  private readonly adminService = inject(AdminService);

  readonly cargando = signal(true);
  readonly error = signal<string | null>(null);
  readonly datos = signal<CobrosNegocio | null>(null);

  // Si el cobro más reciente está rechazado, se avisa arriba con el motivo y qué hacer.
  readonly rechazoActual = computed(() => {
    const cobro = this.datos()?.cobros[0];
    return cobro?.estado === 'rechazado' ? cobro : null;
  });

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.adminService.obtenerCobros(this.data.tenantId).subscribe({
      next: (datos) => {
        this.datos.set(datos);
        this.cargando.set(false);
      },
      error: (err) => {
        this.error.set(extraerMensajeError(err));
        this.cargando.set(false);
      },
    });
  }

  dinero(valor: number): string {
    return '$' + Math.round(valor).toLocaleString('es-AR');
  }

  etiqueta(estado: string): string {
    return ETIQUETAS[estado] ?? estado;
  }
}
