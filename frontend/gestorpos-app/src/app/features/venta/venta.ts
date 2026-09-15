import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Producto } from '../../core/models/catalog.models';
import { MedioPago, VentaDto } from '../../core/models/venta.models';
import { CatalogoService } from '../../core/services/catalogo.service';
import { VentasService } from '../../core/services/ventas.service';
import { extraerMensajeError } from '../../core/utils/error.util';

interface ItemCarrito {
  producto: Producto;
  cantidad: number;
}

@Component({
  selector: 'app-venta',
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './venta.html',
  styleUrl: './venta.scss',
})
export class Venta implements OnInit {
  private readonly catalogoService = inject(CatalogoService);
  private readonly ventasService = inject(VentasService);
  private readonly snackBar = inject(MatSnackBar);

  readonly cargando = signal(true);
  readonly productos = signal<Producto[]>([]);
  readonly busqueda = signal('');
  readonly carrito = signal<ItemCarrito[]>([]);
  readonly medioPago = signal<MedioPago>('Efectivo');
  readonly telefonoCliente = signal('');
  readonly procesando = signal(false);
  readonly ventaResultado = signal<VentaDto | null>(null);

  readonly productosFiltrados = computed(() => {
    const termino = this.busqueda().trim().toLowerCase();
    const disponibles = this.productos().filter((p) => p.stockActual > 0);
    if (!termino) return disponibles.slice(0, 8);
    return disponibles
      .filter((p) => p.nombre.toLowerCase().includes(termino) || p.sku.toLowerCase().includes(termino))
      .slice(0, 8);
  });

  readonly total = computed(() =>
    this.carrito().reduce((acc, item) => acc + item.producto.precio * item.cantidad, 0),
  );

  ngOnInit(): void {
    this.catalogoService.listarProductos().subscribe({
      next: (productos) => {
        this.productos.set(productos);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }

  agregarAlCarrito(producto: Producto): void {
    const actual = this.carrito();
    const existente = actual.find((i) => i.producto.id === producto.id);

    if (existente) {
      if (existente.cantidad >= producto.stockActual) return;
      this.carrito.set(
        actual.map((i) => (i.producto.id === producto.id ? { ...i, cantidad: i.cantidad + 1 } : i)),
      );
    } else {
      this.carrito.set([...actual, { producto, cantidad: 1 }]);
    }
  }

  cambiarCantidad(item: ItemCarrito, delta: number): void {
    const nuevaCantidad = item.cantidad + delta;
    if (nuevaCantidad <= 0) {
      this.quitarDelCarrito(item);
      return;
    }
    if (nuevaCantidad > item.producto.stockActual) return;

    this.carrito.set(
      this.carrito().map((i) => (i.producto.id === item.producto.id ? { ...i, cantidad: nuevaCantidad } : i)),
    );
  }

  quitarDelCarrito(item: ItemCarrito): void {
    this.carrito.set(this.carrito().filter((i) => i.producto.id !== item.producto.id));
  }

  cobrar(): void {
    if (this.carrito().length === 0 || this.procesando()) return;

    this.procesando.set(true);
    this.ventasService
      .crear({
        items: this.carrito().map((i) => ({ productoId: i.producto.id, cantidad: i.cantidad })),
        medioPago: this.medioPago(),
        telefonoCliente: this.telefonoCliente().trim() || null,
      })
      .subscribe({
        next: (venta) => {
          this.procesando.set(false);
          this.ventaResultado.set(venta);
        },
        error: (err) => {
          this.procesando.set(false);
          this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 5000 });
        },
      });
  }

  nuevaVenta(): void {
    this.carrito.set([]);
    this.telefonoCliente.set('');
    this.ventaResultado.set(null);
    this.busqueda.set('');
    // Refresca stock local para que la siguiente venta valide contra los números actuales.
    this.catalogoService.listarProductos().subscribe((productos) => this.productos.set(productos));
  }

  enviarPorWhatsApp(): void {
    const link = this.ventaResultado()?.whatsAppLink;
    if (link) window.open(link, '_blank');
  }
}
