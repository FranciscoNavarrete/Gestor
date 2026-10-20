import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Presentacion, Producto, UnidadMedida } from '../../core/models/catalog.models';
import { ItemCompraRequest, Proveedor } from '../../core/models/compra.models';
import { CatalogoService } from '../../core/services/catalogo.service';
import { CompraService } from '../../core/services/compra.service';
import { esMedida, formatearCantidad, formatearPesos, simboloUnidad, tieneMasDeTresDecimales } from '../../core/utils/cantidad.util';
import { necesitaSelector } from '../../core/utils/carrito.util';
import { extraerMensajeError } from '../../core/utils/error.util';
import { redondearImporte } from '../../core/utils/precio.util';
import { ProveedorDialog, ProveedorDialogData } from '../configuracion/proveedor-dialog/proveedor-dialog';
import { ReciboCompraDialog } from './recibo-compra-dialog/recibo-compra-dialog';

interface ItemCompra {
  /** Identifica la línea: un mismo producto puede comprarse suelto y en bolsa a la vez. */
  idLinea: number;
  producto: Producto;
  presentacionId: string | null;
  cantidad: number | null;
  costoUnitario: number | null;
}

@Component({
  selector: 'app-compras',
  imports: [
    DecimalPipe,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './compras.html',
  styleUrl: './compras.scss',
})
export class Compras implements OnInit {
  private readonly catalogoService = inject(CatalogoService);
  private readonly compraService = inject(CompraService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly cargando = signal(true);
  readonly proveedores = signal<Proveedor[]>([]);
  readonly proveedorId = signal('');
  readonly productos = signal<Producto[]>([]);
  readonly busqueda = signal('');
  readonly items = signal<ItemCompra[]>([]);
  private contadorLineas = 0;
  readonly registrando = signal(false);

  readonly proveedoresActivos = computed(() => this.proveedores().filter((p) => p.activo));

  readonly productosFiltrados = computed(() => {
    const termino = this.busqueda().trim().toLowerCase();
    if (!termino) return [];
    // Un producto común se agrega una sola vez; uno medido o con presentaciones puede repetirse (suelto y en bolsa).
    const yaAgregados = new Set(this.items().filter((i) => !necesitaSelector(i.producto)).map((i) => i.producto.id));
    return this.productos()
      .filter((p) => p.activo && !yaAgregados.has(p.id))
      .filter((p) => p.nombre.toLowerCase().includes(termino) || p.sku.toLowerCase().includes(termino))
      .slice(0, 8);
  });

  readonly total = computed(() => redondearImporte(this.items().reduce((acc, item) => acc + this.subtotal(item), 0)));

  readonly itemsValidos = computed(() => this.items().every((i) => this.cantidadValida(i) && (i.costoUnitario ?? -1) >= 0));

  ngOnInit(): void {
    this.cargarProveedores();
    this.catalogoService.listarProductos().subscribe((productos) => this.productos.set(productos));
  }

  private cargarProveedores(): void {
    this.cargando.set(true);
    this.compraService.listarProveedores().subscribe({
      next: (proveedores) => {
        this.proveedores.set(proveedores);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }

  nuevoProveedor(): void {
    const data: ProveedorDialogData = { proveedor: null };
    this.dialog
      .open(ProveedorDialog, { data, width: '420px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((proveedor: Proveedor | undefined) => {
        if (!proveedor) return;
        this.proveedores.update((actual) => [...actual, proveedor]);
        this.proveedorId.set(proveedor.id);
      });
  }

  agregarProducto(producto: Producto): void {
    this.items.update((actual) => [
      ...actual,
      { idLinea: ++this.contadorLineas, producto, presentacionId: null, cantidad: 1, costoUnitario: producto.costo },
    ]);
    this.busqueda.set('');
  }

  quitarItem(item: ItemCompra): void {
    this.items.update((actual) => actual.filter((i) => i.idLinea !== item.idLinea));
  }

  actualizarCantidad(item: ItemCompra, cantidad: number | null): void {
    this.items.update((actual) => actual.map((i) => (i.idLinea === item.idLinea ? { ...i, cantidad } : i)));
  }

  actualizarCosto(item: ItemCompra, costo: number | null): void {
    this.items.update((actual) => actual.map((i) => (i.idLinea === item.idLinea ? { ...i, costoUnitario: costo } : i)));
  }

  /** Cambia entre comprar suelto (por kilo, litro, metro o unidad) y una presentación; propone el costo que corresponde. */
  elegirPresentacion(item: ItemCompra, presentacionId: string | null): void {
    const presentacion = item.producto.presentaciones.find((p) => p.id === presentacionId) ?? null;
    const costo = presentacion ? redondearImporte(item.producto.costo * presentacion.cantidadBase) : item.producto.costo;
    this.items.update((actual) =>
      actual.map((i) => (i.idLinea === item.idLinea ? { ...i, presentacionId: presentacion?.id ?? null, costoUnitario: costo, cantidad: 1 } : i)),
    );
  }

  esMedida(unidad: UnidadMedida): boolean {
    return esMedida(unidad);
  }

  simboloDe(item: ItemCompra): string {
    return simboloUnidad(item.producto.unidad);
  }

  presentacionDe(item: ItemCompra): Presentacion | null {
    return item.producto.presentaciones.find((p) => p.id === item.presentacionId) ?? null;
  }

  /** Solo el suelto de un producto medido admite decimales; presentaciones y unidades se compran enteras. */
  admiteDecimales(item: ItemCompra): boolean {
    return esMedida(item.producto.unidad) && item.presentacionId === null;
  }

  private cantidadValida(item: ItemCompra): boolean {
    const c = item.cantidad ?? 0;
    if (c <= 0) return false;
    return this.admiteDecimales(item) ? !tieneMasDeTresDecimales(c) : Number.isInteger(c);
  }

  subtotal(item: ItemCompra): number {
    return redondearImporte((item.cantidad ?? 0) * (item.costoUnitario ?? 0));
  }

  etiquetaCosto(item: ItemCompra): string {
    if (item.presentacionId) return 'Costo por ' + (this.presentacionDe(item)?.nombre ?? 'presentación');
    return esMedida(item.producto.unidad) ? `Costo por ${simboloUnidad(item.producto.unidad)}` : 'Costo u.';
  }

  /** "Suma 30 kg · el costo queda en $2800 por kg": lo que va a pasar con el stock y el costo al confirmar. */
  efecto(item: ItemCompra): string | null {
    const cantidad = item.cantidad ?? 0;
    const costo = item.costoUnitario ?? 0;
    const presentacion = this.presentacionDe(item);
    if (cantidad <= 0) return null;
    const base = cantidad * (presentacion?.cantidadBase ?? 1);
    if (!presentacion && !esMedida(item.producto.unidad)) return null;
    const suma = `Suma ${formatearCantidad(Math.round(base * 1000) / 1000, item.producto.unidad)}`;
    const porUnidad = presentacion ? redondearImporte(costo / presentacion.cantidadBase) : costo;
    const unidad = item.producto.unidad === 'Unidad' ? 'u.' : simboloUnidad(item.producto.unidad);
    return `${suma} · el costo queda en ${formatearPesos(porUnidad)} por ${unidad}`;
  }

  registrarCompra(): void {
    if (!this.proveedorId() || this.items().length === 0 || !this.itemsValidos() || this.registrando()) return;

    this.registrando.set(true);
    const items: ItemCompraRequest[] = this.items().map((i) => ({
      productoId: i.producto.id,
      cantidad: i.cantidad!,
      costoUnitario: i.costoUnitario!,
      presentacionId: i.presentacionId,
    }));

    this.compraService.crearCompra({ proveedorId: this.proveedorId(), items }).subscribe({
      next: (compra) => {
        this.registrando.set(false);
        this.items.set([]);
        this.dialog.open(ReciboCompraDialog, { data: compra, width: '480px', maxWidth: '95vw' });
      },
      error: (err) => {
        this.registrando.set(false);
        this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 });
      },
    });
  }
}
