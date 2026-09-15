import { Component, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
import { forkJoin } from 'rxjs';
import { Categoria, Producto } from '../../core/models/catalog.models';
import { CatalogoService } from '../../core/services/catalogo.service';
import { extraerMensajeError } from '../../core/utils/error.util';
import { AjustarStockDialog } from './ajustar-stock-dialog/ajustar-stock-dialog';
import { ImportarResultadoDialog } from './importar-resultado-dialog/importar-resultado-dialog';
import { ProductoDialog, ProductoDialogData } from './producto-dialog/producto-dialog';

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
  ],
  templateUrl: './productos.html',
  styleUrl: './productos.scss',
})
export class Productos implements OnInit {
  private readonly catalogoService = inject(CatalogoService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  @ViewChild('inputArchivo') private inputArchivo?: ElementRef<HTMLInputElement>;

  readonly cargando = signal(true);
  readonly importando = signal(false);
  readonly productos = signal<Producto[]>([]);
  readonly categorias = signal<Categoria[]>([]);
  readonly soloBajoStock = signal(false);
  readonly busqueda = signal('');

  readonly productosFiltrados = computed(() => {
    const termino = this.busqueda().trim().toLowerCase();
    if (!termino) return this.productos();

    return this.productos().filter(
      (p) =>
        p.nombre.toLowerCase().includes(termino) ||
        p.sku.toLowerCase().includes(termino) ||
        (p.categoriaNombre?.toLowerCase().includes(termino) ?? false),
    );
  });

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    forkJoin({
      productos: this.catalogoService.listarProductos(this.soloBajoStock()),
      categorias: this.catalogoService.listarCategorias(),
    }).subscribe({
      next: ({ productos, categorias }) => {
        this.productos.set(productos);
        this.categorias.set(categorias);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }

  toggleBajoStock(): void {
    this.soloBajoStock.set(!this.soloBajoStock());
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
        this.cargar();
      },
      error: (err) => {
        this.importando.set(false);
        this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 5000 });
      },
    });
  }
}
