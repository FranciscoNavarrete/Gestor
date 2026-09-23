import { Component, ElementRef, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Categoria, Producto } from '../../core/models/catalog.models';
import { CatalogoService } from '../../core/services/catalogo.service';
import { extraerMensajeError } from '../../core/utils/error.util';
import { AjustarStockDialog } from './ajustar-stock-dialog/ajustar-stock-dialog';
import { ImportarResultadoDialog } from './importar-resultado-dialog/importar-resultado-dialog';
import { ProductoDialog, ProductoDialogData } from './producto-dialog/producto-dialog';

const TAMANO_PAGINA = 20;
const DEBOUNCE_BUSQUEDA_MS = 350;

@Component({
  selector: 'app-productos',
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
    RouterLink,
  ],
  templateUrl: './productos.html',
  styleUrl: './productos.scss',
})
export class Productos implements OnInit, OnDestroy {
  private readonly catalogoService = inject(CatalogoService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  @ViewChild('inputArchivo') private inputArchivo?: ElementRef<HTMLInputElement>;
  private debounceTimer?: ReturnType<typeof setTimeout>;

  readonly cargando = signal(true);
  readonly importando = signal(false);
  readonly productos = signal<Producto[]>([]);
  readonly categorias = signal<Categoria[]>([]);
  readonly soloBajoStock = signal(false);
  readonly verInactivos = signal(false);
  readonly busqueda = signal('');

  readonly pagina = signal(1);
  readonly totalPaginas = signal(1);
  readonly totalItems = signal(0);

  ngOnInit(): void {
    this.catalogoService.listarCategorias().subscribe((categorias) => this.categorias.set(categorias));
    this.cargar();
  }

  ngOnDestroy(): void {
    clearTimeout(this.debounceTimer);
  }

  cargar(): void {
    this.cargando.set(true);
    this.catalogoService
      .buscarProductos(this.busqueda(), this.soloBajoStock(), this.verInactivos(), this.pagina(), TAMANO_PAGINA)
      .subscribe({
        next: (resultado) => {
          this.productos.set(resultado.items);
          this.totalPaginas.set(resultado.totalPaginas);
          this.totalItems.set(resultado.totalItems);
          this.cargando.set(false);
        },
        error: () => this.cargando.set(false),
      });
  }

  onBusquedaChange(valor: string): void {
    this.busqueda.set(valor);
    clearTimeout(this.debounceTimer);
    this.debounceTimer = setTimeout(() => {
      this.pagina.set(1);
      this.cargar();
    }, DEBOUNCE_BUSQUEDA_MS);
  }

  toggleBajoStock(): void {
    this.soloBajoStock.set(!this.soloBajoStock());
    this.pagina.set(1);
    this.cargar();
  }

  toggleVerInactivos(): void {
    this.verInactivos.set(!this.verInactivos());
    this.pagina.set(1);
    this.cargar();
  }

  paginaAnterior(): void {
    if (this.pagina() <= 1) return;
    this.pagina.set(this.pagina() - 1);
    this.cargar();
  }

  paginaSiguiente(): void {
    if (this.pagina() >= this.totalPaginas()) return;
    this.pagina.set(this.pagina() + 1);
    this.cargar();
  }

  abrirNuevo(): void {
    this.abrirDialogo(null);
  }

  abrirEditar(producto: Producto): void {
    this.abrirDialogo(producto);
  }

  private abrirDialogo(producto: Producto | null): void {
    const data: ProductoDialogData = { producto, categorias: this.categorias() };
    this.dialog
      .open(ProductoDialog, { data, width: '480px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((resultado) => {
        if (resultado) this.cargar();
      });
  }

  ajustarStock(producto: Producto): void {
    this.dialog
      .open(AjustarStockDialog, { data: producto, width: '420px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((resultado) => {
        if (resultado) this.cargar();
      });
  }

  desactivar(producto: Producto): void {
    if (!confirm(`¿Dar de baja "${producto.nombre}"? Ya no vas a poder venderlo.`)) return;

    this.catalogoService.desactivarProducto(producto.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 }),
    });
  }

  reactivar(producto: Producto): void {
    this.catalogoService.activarProducto(producto.id).subscribe({
      next: () => {
        this.cargar();
        this.snackBar.open(`"${producto.nombre}" reactivado`, 'Cerrar', { duration: 3000 });
      },
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 }),
    });
  }

  descargarPlantilla(): void {
    this.catalogoService.descargarPlantillaExcel().subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = 'plantilla-productos.xlsx';
        enlace.click();
        URL.revokeObjectURL(url);
      },
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 }),
    });
  }

  abrirSelectorImportar(): void {
    this.inputArchivo?.nativeElement.click();
  }

  importarArchivoSeleccionado(event: Event): void {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    input.value = ''; // permite volver a elegir el mismo archivo si hace falta reintentar
    if (!archivo) return;

    this.importando.set(true);
    this.catalogoService.importarExcel(archivo).subscribe({
      next: (resultado) => {
        this.importando.set(false);
        this.dialog.open(ImportarResultadoDialog, { data: resultado, width: '460px', maxWidth: '95vw' });
        this.pagina.set(1);
        this.cargar();
      },
      error: (err) => {
        this.importando.set(false);
        this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 5000 });
      },
    });
  }
}
