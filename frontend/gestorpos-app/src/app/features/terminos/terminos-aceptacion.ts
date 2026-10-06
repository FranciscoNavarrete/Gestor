import { AfterViewChecked, Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Terminos } from '../../core/models/terminos.models';
import { AuthService } from '../../core/services/auth.service';
import { TerminosService } from '../../core/services/terminos.service';
import { extraerMensajeError } from '../../core/utils/error.util';

/** Pantalla que ve el dueño del negocio antes de usar el sistema, hasta que acepta los términos vigentes. */
@Component({
  selector: 'app-terminos-aceptacion',
  imports: [MatButtonModule, MatCardModule, MatCheckboxModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './terminos-aceptacion.html',
  styleUrl: './terminos-aceptacion.scss',
})
export class TerminosAceptacion implements OnInit, AfterViewChecked {
  private readonly terminosService = inject(TerminosService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  @ViewChild('texto') private texto?: ElementRef<HTMLElement>;

  readonly cargando = signal(true);
  readonly errorCarga = signal(false);
  readonly terminos = signal<Terminos | null>(null);
  readonly leidoHastaElFinal = signal(false);
  readonly acepto = signal(false);
  readonly enviando = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    // Quien no tiene nada pendiente (o no llegó por el login) no tiene nada que hacer acá.
    if (!this.authService.terminosPendientes()) {
      void this.router.navigate(['/dashboard']);
      return;
    }
    this.cargar();
  }

  // Si el texto entra entero en la caja no hay nada para desplazar: se da por leído.
  ngAfterViewChecked(): void {
    const el = this.texto?.nativeElement;
    if (el && !this.leidoHastaElFinal() && el.scrollHeight <= el.clientHeight + 8) {
      queueMicrotask(() => this.leidoHastaElFinal.set(true));
    }
  }

  cargar(): void {
    this.cargando.set(true);
    this.errorCarga.set(false);
    this.terminosService.vigentes().subscribe({
      next: (t) => {
        this.terminos.set(t);
        this.cargando.set(false);
      },
      error: () => {
        this.errorCarga.set(true);
        this.cargando.set(false);
      },
    });
  }

  alDesplazar(): void {
    const el = this.texto?.nativeElement;
    if (el && el.scrollTop + el.clientHeight >= el.scrollHeight - 8) this.leidoHastaElFinal.set(true);
  }

  aceptar(): void {
    if (!this.acepto() || this.enviando()) return;
    this.enviando.set(true);
    this.error.set(null);
    this.terminosService.aceptar().subscribe({
      next: () => {
        this.authService.marcarTerminosAceptados();
        void this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.enviando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }

  salir(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
