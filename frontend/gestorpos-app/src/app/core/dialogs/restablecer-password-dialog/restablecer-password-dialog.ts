import { Clipboard } from '@angular/cdk/clipboard';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PasswordTemporal } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';
import { extraerMensajeError } from '../../utils/error.util';

export interface RestablecerPasswordDialogData {
  tipo: 'negocio' | 'usuario';
  id: string;
  nombre: string;
  telefono?: string | null;
}

/** El operador restablece una contraseña: confirma, y se le muestra una temporal (una sola vez) para pasarla al cliente. */
@Component({
  selector: 'app-restablecer-password-dialog',
  imports: [MatButtonModule, MatDialogModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './restablecer-password-dialog.html',
  styleUrl: './restablecer-password-dialog.scss',
})
export class RestablecerPasswordDialog {
  readonly data = inject<RestablecerPasswordDialogData>(MAT_DIALOG_DATA);
  private readonly adminService = inject(AdminService);
  private readonly clipboard = inject(Clipboard);
  private readonly snackBar = inject(MatSnackBar);

  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly resultado = signal<PasswordTemporal | null>(null);

  restablecer(): void {
    if (this.guardando()) return;
    this.guardando.set(true);
    this.error.set(null);
    const llamada =
      this.data.tipo === 'negocio'
        ? this.adminService.restablecerPasswordNegocio(this.data.id)
        : this.adminService.restablecerPasswordUsuario(this.data.id);
    llamada.subscribe({
      next: (r) => {
        this.guardando.set(false);
        this.resultado.set(r);
      },
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }

  copiar(texto: string, etiqueta: string): void {
    this.clipboard.copy(texto);
    this.snackBar.open(`${etiqueta} copiada`, 'OK', { duration: 2500 });
  }

  linkWhatsapp(r: PasswordTemporal): string {
    const mensaje = `Hola ${r.nombre}! Tu contraseña temporal de GestorPOS es: ${r.passwordTemporal}\nIngresá con tu email (${r.email}) y elegí una contraseña nueva.`;
    return `https://wa.me/?text=${encodeURIComponent(mensaje)}`;
  }
}
