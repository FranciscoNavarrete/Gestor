import { Clipboard } from '@angular/cdk/clipboard';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { LinkPago } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';
import { extraerMensajeError } from '../../utils/error.util';
import { RefrescoAutomatico } from '../../utils/refresco-automatico';
import { QrDialog } from '../qr-dialog/qr-dialog';

export interface LinkPagoDialogData {
  tenantId: string;
  nombre: string;
}

@Component({
  selector: 'app-link-pago-dialog',
  imports: [MatButtonModule, MatDialogModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './link-pago-dialog.html',
  styleUrl: './link-pago-dialog.scss',
})
export class LinkPagoDialog implements OnInit, OnDestroy {
  readonly data = inject<LinkPagoDialogData>(MAT_DIALOG_DATA);
  private readonly adminService = inject(AdminService);
  private readonly dialog = inject(MatDialog);
  private readonly clipboard = inject(Clipboard);
  private readonly snackBar = inject(MatSnackBar);

  readonly cargando = signal(true);
  readonly error = signal<string | null>(null);
  readonly datos = signal<LinkPago | null>(null);
  readonly refresco = new RefrescoAutomatico(() => this.cargar(true));

  ngOnInit(): void {
    this.cargar();
  }

  ngOnDestroy(): void {
    this.refresco.destruir();
  }

  cargar(silencioso = false): void {
    if (!silencioso) {
      this.cargando.set(true);
      this.error.set(null);
    }
    this.adminService.obtenerLinkPago(this.data.tenantId).subscribe({
      next: (datos) => {
        this.datos.set(datos);
        this.error.set(null);
        this.cargando.set(false);
        this.refresco.evaluar(datos.estado === 'pending');
      },
      error: (err) => {
        // Un refresco automático que falla no pisa lo que ya se está mostrando.
        if (!silencioso) this.error.set(extraerMensajeError(err));
        this.cargando.set(false);
      },
    });
  }

  copiar(link: string): void {
    this.clipboard.copy(link);
    this.snackBar.open('Link copiado', 'OK', { duration: 2500 });
  }

  linkWhatsapp(link: string): string {
    const mensaje = `Hola! Para suscribirte a ${this.data.nombre}, entrá acá: ${link}`;
    return `https://wa.me/?text=${encodeURIComponent(mensaje)}`;
  }

  abrirQr(link: string): void {
    this.dialog.open(QrDialog, { data: { nombre: this.data.nombre, link }, width: '300px' });
  }
}
