import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AceptacionTerminos } from '../../models/terminos.models';
import { AdminService } from '../../services/admin.service';
import { extraerMensajeError } from '../../utils/error.util';

export interface TerminosRegistroDialogData {
  tenantId: string;
  nombre: string;
}

/** Las constancias de aceptación de los términos de un negocio: quién, cuándo y desde dónde. */
@Component({
  selector: 'app-terminos-registro-dialog',
  imports: [DatePipe, MatButtonModule, MatDialogModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './terminos-registro-dialog.html',
  styleUrl: './terminos-registro-dialog.scss',
})
export class TerminosRegistroDialog implements OnInit {
  readonly data = inject<TerminosRegistroDialogData>(MAT_DIALOG_DATA);
  private readonly adminService = inject(AdminService);

  readonly cargando = signal(true);
  readonly error = signal<string | null>(null);
  readonly constancias = signal<AceptacionTerminos[]>([]);

  readonly delCliente = computed(() => this.constancias().find((c) => c.origen === 'Cliente') ?? null);
  readonly delVendedor = computed(() => this.constancias().find((c) => c.origen === 'Vendedor') ?? null);

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.adminService.obtenerTerminos(this.data.tenantId).subscribe({
      next: (constancias) => {
        this.constancias.set(constancias);
        this.cargando.set(false);
      },
      error: (err) => {
        this.error.set(extraerMensajeError(err));
        this.cargando.set(false);
      },
    });
  }
}
