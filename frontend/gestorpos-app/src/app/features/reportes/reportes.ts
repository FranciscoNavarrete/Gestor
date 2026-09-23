import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Producto } from '../../core/models/catalog.models';
import { FEATURE_CUENTA_CORRIENTE } from '../../core/models/cuenta-corriente';
import { MovimientoStock } from '../../core/models/movimiento-stock.models';
import { DeudaClienteDto, DeudasDto, GananciasDto, RankingClienteDto } from '../../core/models/reportes.models';
import { VentaResumenDto } from '../../core/models/venta.models';
import { AuthService } from '../../core/services/auth.service';
import { CatalogoService } from '../../core/services/catalogo.service';
import { MovimientoStockService } from '../../core/services/movimiento-stock.service';
import { ReportesService } from '../../core/services/reportes.service';
import { VentasService } from '../../core/services/ventas.service';
import { extraerMensajeError } from '../../core/utils/error.util';
import { fechaAIso, hoy, isoAFecha, restarMeses } from '../../core/utils/fecha.util';

const TAMANO_PAGINA_STOCK = 20;
const TAMANO_PAGINA_MOVIMIENTOS = 20;
const MAX_MESES_RANGO_FECHAS = 2;

type Vista = 'ventas' | 'stock' | 'movimientos' | 'clientes' | 'deudas';

@Component({
  selector: 'app-reportes',
  imports: [
    DatePipe,
    DecimalPipe,
    FormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSlideToggleModule,
  ],
  templateUrl: './reportes.html',
  styleUrl: './reportes.scss',
})
export class Reportes implements OnInit {
  private readonly ventasService = inject(VentasService);
  private readonly reportesService = inject(ReportesService);
  private readonly catalogoService = inject(CatalogoService);
  private readonly movimientoStockService = inject(MovimientoStockService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  readonly tieneCuentaCorriente = this.authService.tieneFeature(FEATURE_CUENTA_CORRIENTE);

  readonly vista = signal<Vista>('ventas');
  readonly exportando = signal(false);
  readonly hoy = hoy();

  // --- Reporte de ventas ---
  readonly desde = signal('');
  readonly hasta = signal('');
  readonly desdeFecha = computed(() => isoAFecha(this.desde()));
  readonly hastaFecha = computed(() => isoAFecha(this.hasta()));
  readonly maxDesdeFecha = computed(() => this.hastaFecha() ?? this.hoy);
  readonly minDesdeFecha = computed(() => restarMeses(this.maxDesdeFecha(), MAX_MESES_RANGO_FECHAS));
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

  // --- Movimientos de stock ---
  readonly productosParaFiltro = signal<Producto[]>([]);
  readonly filtroProductoId = signal('');
  readonly desdeMovimientos = signal('');
  readonly hastaMovimientos = signal('');
  readonly desdeMovimientosFecha = computed(() => isoAFecha(this.desdeMovimientos()));
  readonly hastaMovimientosFecha = computed(() => isoAFecha(this.hastaMovimientos()));
  readonly maxDesdeMovimientosFecha = computed(() => this.hastaMovimientosFecha() ?? this.hoy);
  readonly minDesdeMovimientosFecha = computed(() => restarMeses(this.maxDesdeMovimientosFecha(), MAX_MESES_RANGO_FECHAS));
  readonly cargandoMovimientos = signal(true);
  readonly movimientos = signal<MovimientoStock[]>([]);
  readonly paginaMovimientos = signal(1);
  readonly totalPaginasMovimientos = signal(1);
  readonly totalItemsMovimientos = signal(0);

  // --- Ranking de clientes ---
  readonly desdeClientes = signal('');
  readonly hastaClientes = signal('');
  readonly desdeClientesFecha = computed(() => isoAFecha(this.desdeClientes()));
  readonly hastaClientesFecha = computed(() => isoAFecha(this.hastaClientes()));
  readonly maxDesdeClientesFecha = computed(() => this.hastaClientesFecha() ?? this.hoy);
  readonly minDesdeClientesFecha = computed(() => restarMeses(this.maxDesdeClientesFecha(), MAX_MESES_RANGO_FECHAS));
  readonly cargandoClientes = signal(true);
  readonly rankingClientes = signal<RankingClienteDto[]>([]);

  // --- Deudas ---
  readonly cargandoDeudas = signal(true);
  readonly deudas = signal<DeudasDto | null>(null);

  ngOnInit(): void {
    this.cargarVentas();
    this.cargarStock();
    this.cargarMovimientos();
    this.cargarClientes();
    if (this.tieneCuentaCorriente) this.cargarDeudas();
    this.catalogoService.listarProductos().subscribe((productos) => this.productosParaFiltro.set(productos));
  }

  cambiarVista(vista: Vista): void {
    this.vista.set(vista);
  }

  // --- Ventas ---

  onDesdeChange(fecha: Date | null): void {
    this.desde.set(fechaAIso(fecha));
    this.cargarVentas();
  }

  onHastaChange(fecha: Date | null): void {
    this.hasta.set(fechaAIso(fecha));
    this.desde.set(this.ajustarDesde(this.desde(), this.hasta()));
    this.cargarVentas();
  }

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
      .buscarProductos(this.busquedaStock(), this.soloBajoStock(), false, this.paginaStock(), TAMANO_PAGINA_STOCK)
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

  // --- Movimientos de stock ---

  cargarMovimientos(): void {
    this.cargandoMovimientos.set(true);
    this.movimientoStockService
      .buscar(
        this.filtroProductoId() || undefined,
        this.desdeMovimientos() || undefined,
        this.hastaMovimientos() || undefined,
        this.paginaMovimientos(),
        TAMANO_PAGINA_MOVIMIENTOS,
      )
      .subscribe({
        next: (resultado) => {
          this.movimientos.set(resultado.items);
          this.totalPaginasMovimientos.set(resultado.totalPaginas);
          this.totalItemsMovimientos.set(resultado.totalItems);
          this.cargandoMovimientos.set(false);
        },
        error: () => this.cargandoMovimientos.set(false),
      });
  }

  onFiltroMovimientosChange(): void {
    this.paginaMovimientos.set(1);
    this.cargarMovimientos();
  }

  onDesdeMovimientosChange(fecha: Date | null): void {
    this.desdeMovimientos.set(fechaAIso(fecha));
    this.onFiltroMovimientosChange();
  }

  onHastaMovimientosChange(fecha: Date | null): void {
    this.hastaMovimientos.set(fechaAIso(fecha));
    this.desdeMovimientos.set(this.ajustarDesde(this.desdeMovimientos(), this.hastaMovimientos()));
    this.onFiltroMovimientosChange();
  }

  limpiarFiltroMovimientos(): void {
    this.filtroProductoId.set('');
    this.desdeMovimientos.set('');
    this.hastaMovimientos.set('');
    this.onFiltroMovimientosChange();
  }

  paginaMovimientosAnterior(): void {
    if (this.paginaMovimientos() <= 1) return;
    this.paginaMovimientos.set(this.paginaMovimientos() - 1);
    this.cargarMovimientos();
  }

  paginaMovimientosSiguiente(): void {
    if (this.paginaMovimientos() >= this.totalPaginasMovimientos()) return;
    this.paginaMovimientos.set(this.paginaMovimientos() + 1);
    this.cargarMovimientos();
  }

  // --- Ranking de clientes ---

  cargarClientes(): void {
    this.cargandoClientes.set(true);
    this.reportesService.rankingClientes(this.desdeClientes() || undefined, this.hastaClientes() || undefined).subscribe({
      next: (ranking) => {
        this.rankingClientes.set(ranking);
        this.cargandoClientes.set(false);
      },
      error: () => this.cargandoClientes.set(false),
    });
  }

  onDesdeClientesChange(fecha: Date | null): void {
    this.desdeClientes.set(fechaAIso(fecha));
    this.cargarClientes();
  }

  onHastaClientesChange(fecha: Date | null): void {
    this.hastaClientes.set(fechaAIso(fecha));
    this.desdeClientes.set(this.ajustarDesde(this.desdeClientes(), this.hastaClientes()));
    this.cargarClientes();
  }

  limpiarFiltroClientes(): void {
    this.desdeClientes.set('');
    this.hastaClientes.set('');
    this.cargarClientes();
  }

  // --- Deudas ---

  cargarDeudas(): void {
    this.cargandoDeudas.set(true);
    this.reportesService.deudas().subscribe({
      next: (deudas) => {
        this.deudas.set(deudas);
        this.cargandoDeudas.set(false);
      },
      error: () => this.cargandoDeudas.set(false),
    });
  }

  exportarDeudasPdf(): void {
    this.descargarPdf(this.reportesService.exportarDeudasPdf(), 'reporte-deudas.pdf');
  }

  abrirClienteDeuda(deuda: DeudaClienteDto): void {
    this.router.navigate(['/clientes', deuda.clienteId]);
  }

  exportarVentasPdf(): void {
    const desde = this.desde() || undefined;
    const hasta = this.hasta() || undefined;
    this.descargarPdf(this.reportesService.exportarVentasPdf(desde, hasta), 'reporte-ventas.pdf');
  }

  exportarStockPdf(): void {
    this.descargarPdf(
      this.reportesService.exportarStockPdf(this.busquedaStock(), this.soloBajoStock()),
      'reporte-stock.pdf',
    );
  }

  // Si "hasta" cambia y deja a "desde" fuera del rango permitido (después de "hasta", o a más de
  // MAX_MESES_RANGO_FECHAS de distancia), lo corrige para que el rango siga siendo válido.
  private ajustarDesde(desdeIso: string, hastaIso: string): string {
    if (!desdeIso) return desdeIso;

    const desdeFecha = isoAFecha(desdeIso)!;
    const hastaFecha = isoAFecha(hastaIso) ?? this.hoy;
    const minPermitido = restarMeses(hastaFecha, MAX_MESES_RANGO_FECHAS);

    if (desdeFecha > hastaFecha) return fechaAIso(hastaFecha);
    if (desdeFecha < minPermitido) return fechaAIso(minPermitido);
    return desdeIso;
  }

  private descargarPdf(descarga: Observable<Blob>, nombreArchivo: string): void {
    if (this.exportando()) return;
    this.exportando.set(true);
    descarga.subscribe({
      next: (blob) => {
        this.exportando.set(false);
        const url = URL.createObjectURL(blob);
        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = nombreArchivo;
        enlace.click();
        URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.exportando.set(false);
        this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 });
      },
    });
  }
}
