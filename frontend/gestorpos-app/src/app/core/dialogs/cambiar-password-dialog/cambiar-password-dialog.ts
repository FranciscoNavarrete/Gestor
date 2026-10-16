import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CambiarPassword } from '../../components/cambiar-password/cambiar-password';
import { AdminAuthService } from '../../services/admin-auth.service';

/** Cambio de contraseña de quien está en el panel interno (operador o vendedor). */
@Component({
  selector: 'app-cambiar-password-dialog',
  imports: [MatButtonModule, MatDialogModule, CambiarPassword],
  template: `
    <h2 mat-dialog-title>Cambiar contraseña</h2>
    <mat-dialog-content>
      <app-cambiar-password [guardar]="guardar" (cambiada)="alCambiar()" />
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [mat-dialog-close]="false">Cancelar</button>
    </mat-dialog-actions>
  `,
})
export class CambiarPasswordDialog {
  private readonly adminAuth = inject(AdminAuthService);
  private readonly dialogRef = inject(MatDialogRef<CambiarPasswordDialog, boolean>);
  private readonly snackBar = inject(MatSnackBar);

  readonly guardar = (actual: string, nueva: string) => this.adminAuth.cambiarPassword(actual, nueva);

  alCambiar(): void {
    this.snackBar.open('Contraseña cambiada', 'OK', { duration: 3000 });
    this.dialogRef.close(true);
  }
}
