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
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Categoria } from '../../core/models/catalog.models';
import { Proveedor } from '../../core/models/compra.models';
import { FEATURE_COMPRAS } from '../../core/models/compras-feature';
import { MedioPagoDto, NegocioDto } from '../../core/models/configuracion.models';
import { FEATURE_FACTURAS_PROVEEDOR } from '../../core/models/facturas-proveedor-feature';
import { AuthService } from '../../core/services/auth.service';
import { CatalogoService } from '../../core/services/catalogo.service';
import { CompraService } from '../../core/services/compra.service';
import { ConfiguracionService } from '../../core/services/configuracion.service';
import { NotificacionPushService } from '../../core/services/notificacion-push.service';
import { extraerMensajeError } from '../../core/utils/error.util';
import { comprimirImagen } from '../../core/utils/imagen.util';
import { CategoriaDialog, CategoriaDialogData } from '../productos/categoria-dialog/categoria-dialog';
import { MedioPagoDialog, MedioPagoDialogData } from './medio-pago-dialog/medio-pago-dialog';
import { ProveedorDialog, ProveedorDialogData } from './proveedor-dialog/proveedor-dialog';

type Vista = 'categorias' | 'proveedores' | 'medios-pago' | 'negocio';

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
    MatSlideToggleModule,
  ],
  templateUrl: './configuracion.html',
  styleUrl: './configuracion.scss',
})
export class Configuracion implements OnInit, OnDestroy {
  private readonly catalogoService = inject(CatalogoService);
  private readonly compraService = inject(CompraService);
  private readonly configuracionService = inject(ConfiguracionService);
  private readonly authService = inject(AuthService);
  private readonly notificacionPushService = inject(NotificacionPushService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  @ViewChild('inputLogo') private inputLogo?: ElementRef<HTMLInputElement>;
  private logoObjectUrl: string | null = null;

  readonly vista = signal<Vista>('categorias');
  readonly tieneCompras = this.authService.tieneFeature(FEATURE_COMPRAS);
  readonly tieneFacturasProveedor = this.authService.tieneFeature(FEATURE_FACTURAS_PROVEEDOR);
  readonly tieneProveedores = this.tieneCompras || this.tieneFacturasProveedor;

  readonly cargandoCategorias = signal(true);
  readonly categorias = signal<Categoria[]>([]);

  readonly cargandoProveedores = signal(true);
  readonly proveedores = signal<Proveedor[]>([]);

  readonly cargandoMediosPago = signal(true);
  readonly mediosPago = signal<MedioPagoDto[]>([]);

  readonly cargandoNegocio = signal(true);
  readonly negocio = signal<NegocioDto | null>(null);
  readonly nombreNegocioForm = signal('');
  readonly telefonoForm = signal('');
  readonly logoUrl = signal<string | null>(null);
  readonly guardandoNegocio = signal(false);
  readonly subiendoLogo = signal(false);

  readonly notificacionesSoportadas = this.notificacionPushService.soportado;
  readonly notificacionesActivas = signal(false);
  readonly cambiandoNotificaciones = signal(false);

  ngOnInit(): void {
    this.cargarCategorias();
    if (this.tieneProveedores) this.cargarProveedores();
    this.cargarMediosPago();
    this.cargarNegocio();
    this.cargarEstadoNotificaciones();
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

  activarCategoria(categoria: Categoria): void {
    this.catalogoService.activarCategoria(categoria.id).subscribe({
      next: () => this.cargarCategorias(),
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 }),
    });
  }

  eliminarCategoria(categoria: Categoria): void {
    if (!confirm(`¿Eliminar la categoría "${categoria.nombre}"? Esta acción no se puede deshacer.`)) return;

    this.catalogoService.eliminarCategoria(categoria.id).subscribe({
      next: () => {
        this.cargarCategorias();
        this.snackBar.open('Categoría eliminada', 'Cerrar', { duration: 3000 });
      },
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 5000 }),
    });
  }

  // --- Proveedores ---

  private cargarProveedores(): void {
    this.cargandoProveedores.set(true);
    this.compraService.listarProveedores().subscribe({
      next: (proveedores) => {
        this.proveedores.set(proveedores);
        this.cargandoProveedores.set(false);
      },
      error: () => this.cargandoProveedores.set(false),
    });
  }

  nuevoProveedor(): void {
    this.abrirDialogoProveedor(null);
  }

  editarProveedor(proveedor: Proveedor): void {
    this.abrirDialogoProveedor(proveedor);
  }

  private abrirDialogoProveedor(proveedor: Proveedor | null): void {
    const data: ProveedorDialogData = { proveedor };
    this.dialog
      .open(ProveedorDialog, { data, width: '420px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((resultado) => {
        if (resultado) this.cargarProveedores();
      });
  }

  desactivarProveedor(proveedor: Proveedor): void {
    if (!confirm(`¿Dar de baja el proveedor "${proveedor.nombre}"?`)) return;

    this.compraService.desactivarProveedor(proveedor.id).subscribe({
      next: () => this.cargarProveedores(),
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 }),
    });
  }

  activarProveedor(proveedor: Proveedor): void {
    this.compraService.activarProveedor(proveedor.id).subscribe({
      next: () => this.cargarProveedores(),
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

  eliminarMedioPago(medioPago: MedioPagoDto): void {
    if (!confirm(`¿Eliminar el medio de pago "${medioPago.nombre}"? Esta acción no se puede deshacer.`)) return;

    this.configuracionService.eliminarMedioPago(medioPago.id).subscribe({
      next: () => {
        this.cargarMediosPago();
        this.snackBar.open('Medio de pago eliminado', 'Cerrar', { duration: 3000 });
      },
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 5000 }),
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

  async logoSeleccionado(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    input.value = '';
    if (!archivo) return;

    this.subiendoLogo.set(true);
    const archivoComprimido = await comprimirImagen(archivo);
    this.configuracionService.subirLogo(archivoComprimido).subscribe({
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

  // --- Notificaciones push ---

  private async cargarEstadoNotificaciones(): Promise<void> {
    this.notificacionesActivas.set(await this.notificacionPushService.estaSuscripto());
  }

  async cambiarNotificaciones(activar: boolean): Promise<void> {
    if (this.cambiandoNotificaciones()) return;

    this.cambiandoNotificaciones.set(true);
    try {
      if (activar) {
        await this.notificacionPushService.suscribirse();
      } else {
        await this.notificacionPushService.desuscribirse();
      }
      this.notificacionesActivas.set(activar);
    } catch (err) {
      this.notificacionesActivas.set(!activar);
      const mensaje = err instanceof Error ? err.message : 'No se pudo cambiar la configuración de notificaciones.';
      this.snackBar.open(mensaje, 'Cerrar', { duration: 4000 });
    } finally {
      this.cambiandoNotificaciones.set(false);
    }
  }
}
