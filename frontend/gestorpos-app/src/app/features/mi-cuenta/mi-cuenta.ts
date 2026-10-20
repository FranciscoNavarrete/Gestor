import { Component, ElementRef, Injector, afterNextRender, computed, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CambiarPassword } from '../../core/components/cambiar-password/cambiar-password';
import { ConfirmDialog } from '../../core/dialogs/confirm-dialog/confirm-dialog';
import { AuthService } from '../../core/services/auth.service';
import { abrirSoporte } from '../../core/utils/soporte.util';

const ETIQUETAS_ROL: Record<string, string> = { Admin: 'Dueño', Cajero: 'Cajero' };

/** Los datos de quien está logueado, el cambio de su contraseña, el soporte y salir. */
@Component({
  selector: 'app-mi-cuenta',
  imports: [MatButtonModule, MatIconModule, CambiarPassword],
  templateUrl: './mi-cuenta.html',
  styleUrl: './mi-cuenta.scss',
})
export class MiCuenta {
  private readonly authService = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);

  readonly nombreUsuario = this.authService.nombreUsuario;
  readonly nombreNegocio = this.authService.nombreNegocio;
  readonly inicial = computed(() => (this.nombreUsuario().trim().charAt(0) || '?').toUpperCase());
  readonly etiquetaRol = computed(() => {
    const rol = this.authService.auth()?.rol ?? '';
    return ETIQUETAS_ROL[rol] ?? rol;
  });
  readonly cambiando = signal(false);
  private readonly formulario = viewChild<ElementRef<HTMLElement>>('formulario');

  // En celular el formulario queda debajo de la barra de abajo: al abrirlo se lo trae a la vista completo.
  abrirFormulario(): void {
    this.cambiando.set(true);
    // Recién después de dibujar el formulario existe el ancla a la que se desplaza.
    afterNextRender(() => this.formulario()?.nativeElement.scrollIntoView({ block: 'end', behavior: 'smooth' }), {
      injector: this.injector,
    });
  }

  readonly guardar = (actual: string, nueva: string) => this.authService.cambiarPassword(actual, nueva);

  alCambiar(): void {
    this.cambiando.set(false);
    this.snackBar.open('Contraseña cambiada', 'OK', { duration: 3000 });
  }

  contactarSoporte(): void {
    abrirSoporte(`Hola, soy de ${this.nombreNegocio()} y necesito ayuda con GestorPOS.`);
  }

  cerrarSesion(): void {
    this.dialog
      .open(ConfirmDialog, {
        data: { titulo: 'Cerrar sesión', mensaje: '¿Seguro que querés salir?', textoConfirmar: 'Cerrar sesión' },
        width: '360px',
      })
      .afterClosed()
      .subscribe((confirmado) => {
        if (!confirmado) return;
        this.authService.logout();
        this.router.navigate(['/login']);
      });
  }
}
