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
import { Producto } from '../../core/models/catalog.models';
import { ItemCompraRequest, Proveedor } from '../../core/models/compra.models';
import { CatalogoService } from '../../core/services/catalogo.service';
import { CompraService } from '../../core/services/compra.service';
import { extraerMensajeError } from '../../core/utils/error.util';
import { ProveedorDialog, ProveedorDialogData } from '../configuracion/proveedor-dialog/proveedor-dialog';
import { ReciboCompraDialog } from './recibo-compra-dialog/recibo-compra-dialog';

interface ItemCompra {
  producto: Producto;
  cantidad: number;
  costoUnitario: number;
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
  readonly registrando = signal(false);

  readonly proveedoresActivos = computed(() => this.proveedores().filter((p) => p.activo));

  readonly productosFiltrados = computed(() => {
    const termino = this.busqueda().trim().toLowerCase();
    if (!termino) return [];
    const yaAgregados = new Set(this.items().map((i) => i.producto.id));
    return this.productos()
      .filter((p) => p.activo && !yaAgregados.has(p.id))
      .filter((p) => p.nombre.toLowerCase().includes(termino) || p.sku.toLowerCase().includes(termino))
      .slice(0, 8);
  });

  readonly total = computed(() =>
    this.items().reduce((acc, item) => acc + item.cantidad * item.costoUnitario, 0),
  );

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
    this.items.update((actual) => [...actual, { producto, cantidad: 1, costoUnitario: producto.costo }]);
    this.busqueda.set('');
  }

  quitarItem(producto: Producto): void {
    this.items.update((actual) => actual.filter((i) => i.producto.id !== producto.id));
  }

  actualizarCantidad(producto: Producto, cantidad: number): void {
    this.items.update((actual) =>
      actual.map((i) => (i.producto.id === producto.id ? { ...i, cantidad: Math.max(1, cantidad || 1) } : i)),
    );
  }

  actualizarCosto(producto: Producto, costo: number): void {
    this.items.update((actual) =>
      actual.map((i) => (i.producto.id === producto.id ? { ...i, costoUnitario: Math.max(0, costo || 0) } : i)),
    );
  }

  subtotal(item: ItemCompra): number {
    return item.cantidad * item.costoUnitario;
  }

  registrarCompra(): void {
    if (!this.proveedorId() || this.items().length === 0 || this.registrando()) return;

    this.registrando.set(true);
    const items: ItemCompraRequest[] = this.items().map((i) => ({
      productoId: i.producto.id,
      cantidad: i.cantidad,
      costoUnitario: i.costoUnitario,
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
