import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin } from 'rxjs';
import { Categoria, Producto } from '../../core/models/catalog.models';
import { CatalogoService } from '../../core/services/catalogo.service';
import { extraerMensajeError } from '../../core/utils/error.util';
import { AjustarStockDialog } from './ajustar-stock-dialog/ajustar-stock-dialog';
import { ProductoDialog, ProductoDialogData } from './producto-dialog/producto-dialog';

@Component({
  selector: 'app-productos',
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
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

  readonly cargando = signal(true);
  readonly productos = signal<Producto[]>([]);
  readonly categorias = signal<Categoria[]>([]);
  readonly soloBajoStock = signal(false);

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
}
