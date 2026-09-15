import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { Producto } from '../../core/models/catalog.models';
import { GananciasDto } from '../../core/models/reportes.models';
import { VentaResumenDto } from '../../core/models/venta.models';
import { CatalogoService } from '../../core/services/catalogo.service';
import { ReportesService } from '../../core/services/reportes.service';
import { VentasService } from '../../core/services/ventas.service';

const TAMANO_PAGINA_STOCK = 20;

type Vista = 'ventas' | 'stock';

@Component({
  selector: 'app-reportes',
  imports: [
    DatePipe,
    DecimalPipe,
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
  templateUrl: './reportes.html',
  styleUrl: './reportes.scss',
})
export class Reportes implements OnInit {
  private readonly ventasService = inject(VentasService);
  private readonly reportesService = inject(ReportesService);
  private readonly catalogoService = inject(CatalogoService);

  readonly vista = signal<Vista>('ventas');

  // --- Reporte de ventas ---
  readonly desde = signal('');
  readonly hasta = signal('');
  readonly cargandoVentas = signal(true);
  readonly ganancias = signal<GananciasDto | null>(null);
  readonly ventas = signal<VentaResumenDto[]>([]);

  // --- Reporte de stock ---
  readonly busquedaStock = signal('');
  readonly soloBajoStock = signal(false);
  readonly cargandoStock = signal(true);
  readonly productosStock = signal<Producto[]>([]);
  readonly paginaStock = signal(1);
  readonly totalPaginasStock = signal(1);
  readonly totalItemsStock = signal(0);
  private debounceStock?: ReturnType<typeof setTimeout>;

  ngOnInit(): void {
    this.cargarVentas();
    this.cargarStock();
  }

  cambiarVista(vista: Vista): void {
    this.vista.set(vista);
  }

  // --- Ventas ---

  cargarVentas(): void {
    this.cargandoVentas.set(true);
    const desde = this.desde() || undefined;
    const hasta = this.hasta() || undefined;

    this.reportesService.ganancias(desde, hasta).subscribe((g) => this.ganancias.set(g));
    this.ventasService.listar(desde, hasta).subscribe({
      next: (ventas) => {
        this.ventas.set(ventas);
        this.cargandoVentas.set(false);
      },
      error: () => this.cargandoVentas.set(false),
    });
  }

  limpiarFiltroFechas(): void {
    this.desde.set('');
    this.hasta.set('');
    this.cargarVentas();
  }

  // --- Stock ---

  cargarStock(): void {
    this.cargandoStock.set(true);
    this.catalogoService
      .buscarProductos(this.busquedaStock(), this.soloBajoStock(), this.paginaStock(), TAMANO_PAGINA_STOCK)
      .subscribe({
        next: (resultado) => {
          this.productosStock.set(resultado.items);
          this.totalPaginasStock.set(resultado.totalPaginas);
          this.totalItemsStock.set(resultado.totalItems);
          this.cargandoStock.set(false);
        },
        error: () => this.cargandoStock.set(false),
      });
  }

  onBusquedaStockChange(valor: string): void {
    this.busquedaStock.set(valor);
    clearTimeout(this.debounceStock);
    this.debounceStock = setTimeout(() => {
      this.paginaStock.set(1);
      this.cargarStock();
    }, 350);
  }

  toggleBajoStock(): void {
    this.soloBajoStock.set(!this.soloBajoStock());
    this.paginaStock.set(1);
    this.cargarStock();
  }

  paginaStockAnterior(): void {
    if (this.paginaStock() <= 1) return;
    this.paginaStock.set(this.paginaStock() - 1);
    this.cargarStock();
  }

  paginaStockSiguiente(): void {
    if (this.paginaStock() >= this.totalPaginasStock()) return;
    this.paginaStock.set(this.paginaStock() + 1);
    this.cargarStock();
  }

  valorizado(producto: Producto): number {
    return producto.stockActual * producto.costo;
  }
}
