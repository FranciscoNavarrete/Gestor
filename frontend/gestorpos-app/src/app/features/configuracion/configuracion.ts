import { Component, ElementRef, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Categoria } from '../../core/models/catalog.models';
import { MedioPagoDto, NegocioDto } from '../../core/models/configuracion.models';
import { AuthService } from '../../core/services/auth.service';
import { CatalogoService } from '../../core/services/catalogo.service';
import { ConfiguracionService } from '../../core/services/configuracion.service';
import { extraerMensajeError } from '../../core/utils/error.util';
import { CategoriaDialog, CategoriaDialogData } from '../productos/categoria-dialog/categoria-dialog';
import { MedioPagoDialog, MedioPagoDialogData } from './medio-pago-dialog/medio-pago-dialog';

type Vista = 'categorias' | 'medios-pago' | 'negocio';

@Component({
  selector: 'app-configuracion',
  imports: [
    FormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './configuracion.html',
  styleUrl: './configuracion.scss',
})
export class Configuracion implements OnInit, OnDestroy {
  private readonly catalogoService = inject(CatalogoService);
  private readonly configuracionService = inject(ConfiguracionService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  @ViewChild('inputLogo') private inputLogo?: ElementRef<HTMLInputElement>;
  private logoObjectUrl: string | null = null;

  readonly vista = signal<Vista>('categorias');

  readonly cargandoCategorias = signal(true);
  readonly categorias = signal<Categoria[]>([]);

  readonly cargandoMediosPago = signal(true);
  readonly mediosPago = signal<MedioPagoDto[]>([]);

  readonly cargandoNegocio = signal(true);
  readonly negocio = signal<NegocioDto | null>(null);
  readonly nombreNegocioForm = signal('');
  readonly telefonoForm = signal('');
  readonly logoUrl = signal<string | null>(null);
  readonly guardandoNegocio = signal(false);
  readonly subiendoLogo = signal(false);

  ngOnInit(): void {
    this.cargarCategorias();
    this.cargarMediosPago();
    this.cargarNegocio();
  }

  ngOnDestroy(): void {
    if (this.logoObjectUrl) URL.revokeObjectURL(this.logoObjectUrl);
  }

  cambiarVista(vista: Vista): void {
    this.vista.set(vista);
  }

  // --- Categorías ---

  private cargarCategorias(): void {
    this.cargandoCategorias.set(true);
    this.catalogoService.listarCategorias().subscribe({
      next: (categorias) => {
        this.categorias.set(categorias);
        this.cargandoCategorias.set(false);
      },
      error: () => this.cargandoCategorias.set(false),
    });
  }

  nuevaCategoria(): void {
    this.abrirDialogoCategoria(null);
  }

  editarCategoria(categoria: Categoria): void {
    this.abrirDialogoCategoria(categoria);
  }

  private abrirDialogoCategoria(categoria: Categoria | null): void {
    const data: CategoriaDialogData = { categoria };
    this.dialog
      .open(CategoriaDialog, { data, width: '420px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((resultado) => {
        if (resultado) this.cargarCategorias();
      });
  }

  desactivarCategoria(categoria: Categoria): void {
    if (!confirm(`¿Dar de baja la categoría "${categoria.nombre}"?`)) return;

    this.catalogoService.desactivarCategoria(categoria.id).subscribe({
      next: () => this.cargarCategorias(),
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 }),
    });
  }

  // --- Métodos de pago ---

  private cargarMediosPago(): void {
    this.cargandoMediosPago.set(true);
    this.configuracionService.listarMediosPago().subscribe({
      next: (mediosPago) => {
        this.mediosPago.set(mediosPago);
        this.cargandoMediosPago.set(false);
      },
      error: () => this.cargandoMediosPago.set(false),
    });
  }

  nuevoMedioPago(): void {
    this.abrirDialogoMedioPago(null);
  }

  editarMedioPago(medioPago: MedioPagoDto): void {
    this.abrirDialogoMedioPago(medioPago);
  }

  private abrirDialogoMedioPago(medioPago: MedioPagoDto | null): void {
    const data: MedioPagoDialogData = { medioPago };
    this.dialog
      .open(MedioPagoDialog, { data, width: '420px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((resultado) => {
        if (resultado) this.cargarMediosPago();
      });
  }

  desactivarMedioPago(medioPago: MedioPagoDto): void {
    if (!confirm(`¿Dar de baja el medio de pago "${medioPago.nombre}"?`)) return;

    this.configuracionService.desactivarMedioPago(medioPago.id).subscribe({
      next: () => this.cargarMediosPago(),
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 }),
    });
  }

  activarMedioPago(medioPago: MedioPagoDto): void {
    this.configuracionService.activarMedioPago(medioPago.id).subscribe({
      next: () => this.cargarMediosPago(),
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 }),
    });
  }

  // --- Negocio ---

  private cargarNegocio(): void {
    this.cargandoNegocio.set(true);
    this.configuracionService.obtenerNegocio().subscribe({
      next: (negocio) => {
        this.negocio.set(negocio);
        this.nombreNegocioForm.set(negocio.nombre);
        this.telefonoForm.set(negocio.telefono ?? '');
        this.cargandoNegocio.set(false);
        if (negocio.tieneLogo) this.cargarLogo();
      },
      error: () => this.cargandoNegocio.set(false),
    });
  }

  private cargarLogo(): void {
    this.configuracionService.obtenerLogoBlob().subscribe({
      next: (blob) => {
        if (this.logoObjectUrl) URL.revokeObjectURL(this.logoObjectUrl);
        this.logoObjectUrl = URL.createObjectURL(blob);
        this.logoUrl.set(this.logoObjectUrl);
      },
      error: () => this.logoUrl.set(null),
    });
  }

  guardarNegocio(): void {
    if (this.guardandoNegocio()) return;

    this.guardandoNegocio.set(true);
    this.configuracionService
      .actualizarNegocio({ nombre: this.nombreNegocioForm(), telefono: this.telefonoForm().trim() || null })
      .subscribe({
        next: (negocio) => {
          this.guardandoNegocio.set(false);
          this.negocio.set(negocio);
          this.authService.actualizarNombreNegocio(negocio.nombre);
          this.snackBar.open('Datos del negocio guardados', 'Cerrar', { duration: 3000 });
        },
        error: (err) => {
          this.guardandoNegocio.set(false);
          this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 });
        },
      });
  }

  abrirSelectorLogo(): void {
    this.inputLogo?.nativeElement.click();
  }

  logoSeleccionado(event: Event): void {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    input.value = '';
    if (!archivo) return;

    this.subiendoLogo.set(true);
    this.configuracionService.subirLogo(archivo).subscribe({
      next: (negocio) => {
        this.subiendoLogo.set(false);
        this.negocio.set(negocio);
        this.cargarLogo();
      },
      error: (err) => {
        this.subiendoLogo.set(false);
        this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 });
      },
    });
  }

  quitarLogo(): void {
    if (!confirm('¿Sacar el logo del negocio?')) return;

    this.configuracionService.quitarLogo().subscribe({
      next: (negocio) => {
        this.negocio.set(negocio);
        if (this.logoObjectUrl) URL.revokeObjectURL(this.logoObjectUrl);
        this.logoObjectUrl = null;
        this.logoUrl.set(null);
      },
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 }),
    });
  }
}
