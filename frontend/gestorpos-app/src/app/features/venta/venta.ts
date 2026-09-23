import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
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
import { Cliente } from '../../core/models/cliente.models';
import { MedioPagoDto } from '../../core/models/configuracion.models';
import { FEATURE_CUENTA_CORRIENTE, MEDIO_PAGO_A_CUENTA } from '../../core/models/cuenta-corriente';
import { CajaDto, RankingProductoDto } from '../../core/models/reportes.models';
import { MedioPago, VentaDto } from '../../core/models/venta.models';
import { AuthService } from '../../core/services/auth.service';
import { CajaService } from '../../core/services/caja.service';
import { CatalogoService } from '../../core/services/catalogo.service';
import { ConfiguracionService } from '../../core/services/configuracion.service';
import { ReportesService } from '../../core/services/reportes.service';
import { VentasService } from '../../core/services/ventas.service';
import { extraerMensajeError } from '../../core/utils/error.util';
import { ClientePickerDialog, ClientePickerResultado } from './cliente-picker-dialog/cliente-picker-dialog';

const TOP_MAS_VENDIDOS = 6;

interface ItemCarrito {
  producto: Producto;
  cantidad: number;
}

interface UltimoAgregado {
  nombre: string;
  cantidad: number;
  subtotal: number;
}

const DURACION_ULTIMO_AGREGADO_MS = 1200;

@Component({
  selector: 'app-venta',
  imports: [
    FormsModule,
    RouterLink,
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
export class Venta implements OnInit, OnDestroy {
  private readonly catalogoService = inject(CatalogoService);
  private readonly ventasService = inject(VentasService);
  private readonly reportesService = inject(ReportesService);
  private readonly cajaService = inject(CajaService);
  private readonly configuracionService = inject(ConfiguracionService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly medioPagoACuenta = MEDIO_PAGO_A_CUENTA;
  readonly tieneCuentaCorriente = this.authService.tieneFeature(FEATURE_CUENTA_CORRIENTE);

  private timerUltimoAgregado?: ReturnType<typeof setTimeout>;

  readonly cargando = signal(true);
  readonly cargandoCaja = signal(true);
  readonly cajaAbierta = signal(false);
  readonly productos = signal<Producto[]>([]);
  readonly mediosPago = signal<MedioPagoDto[]>([]);
  readonly rankingTop = signal<RankingProductoDto[]>([]);
  readonly busqueda = signal('');
  readonly carrito = signal<ItemCarrito[]>([]);
  readonly carritoExpandido = signal(false);
  readonly medioPago = signal<MedioPago>('Efectivo');
  readonly nombreCliente = signal('');
  readonly telefonoCliente = signal('');
  readonly clienteSeleccionado = signal<Cliente | null>(null);
  readonly montoRecibido = signal<number | null>(null);
  readonly procesando = signal(false);
  readonly ventaResultado = signal<VentaDto | null>(null);
  readonly ultimoAgregado = signal<UltimoAgregado | null>(null);

  readonly masVendidos = computed(() => {
    const porId = new Map(this.productos().map((p) => [p.id, p]));
    return this.rankingTop()
      .map((r) => porId.get(r.productoId))
      .filter((p): p is Producto => !!p && p.stockActual > 0);
  });

  readonly productosFiltrados = computed(() => {
    const termino = this.busqueda().trim().toLowerCase();
    const disponibles = this.productos().filter((p) => p.stockActual > 0);
    if (!termino) return disponibles.slice(0, 8);
    return disponibles
      .filter(
        (p) =>
          p.nombre.toLowerCase().includes(termino) ||
          p.sku.toLowerCase().includes(termino) ||
          (p.categoriaNombre?.toLowerCase().includes(termino) ?? false),
      )
      .slice(0, 8);
  });

  readonly total = computed(() =>
    this.carrito().reduce((acc, item) => acc + item.producto.precio * item.cantidad, 0),
  );

  readonly cantidadItems = computed(() => this.carrito().reduce((acc, item) => acc + item.cantidad, 0));

  readonly vuelto = computed(() => {
    const recibido = this.montoRecibido();
    return recibido == null ? null : recibido - this.total();
  });

  readonly esACuenta = computed(() => this.medioPago() === MEDIO_PAGO_A_CUENTA);
  readonly faltaClienteParaACuenta = computed(() => this.esACuenta() && !this.telefonoCliente().trim());
  readonly hayCliente = computed(() => !!this.nombreCliente().trim() || !!this.telefonoCliente().trim());

  cantidadEnCarrito(productoId: string): number {
    return this.carrito().find((i) => i.producto.id === productoId)?.cantidad ?? 0;
  }

  ngOnInit(): void {
    this.cargarProductos();
    this.reportesService.rankingProductos(TOP_MAS_VENDIDOS).subscribe((ranking) => this.rankingTop.set(ranking));
    this.configuracionService
      .listarMediosPago()
      .subscribe((medios) => this.mediosPago.set(medios.filter((m) => m.activo)));
    this.cajaService.actual().subscribe({
      next: (caja: CajaDto | null) => {
        this.cajaAbierta.set(caja !== null);
        this.cargandoCaja.set(false);
      },
      error: () => this.cargandoCaja.set(false),
    });
  }

  ngOnDestroy(): void {
    clearTimeout(this.timerUltimoAgregado);
  }

  private cargarProductos(mostrarSpinner = true): void {
    if (mostrarSpinner) this.cargando.set(true);
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

    let nuevaCantidad: number;
    if (existente) {
      if (existente.cantidad >= producto.stockActual) return;
      nuevaCantidad = existente.cantidad + 1;
      this.carrito.set(
        actual.map((i) => (i.producto.id === producto.id ? { ...i, cantidad: nuevaCantidad } : i)),
      );
    } else {
      nuevaCantidad = 1;
      this.carrito.set([...actual, { producto, cantidad: 1 }]);
    }

    this.mostrarUltimoAgregado(producto.nombre, nuevaCantidad, producto.precio * nuevaCantidad);
  }

  // Confirmación breve en la barra del carrito ("+1 Coca Cola · 3 · $6300") para que el vendedor
  // sepa qué cargó sin tener que abrir el carrito — desaparece sola y vuelve al resumen normal.
  private mostrarUltimoAgregado(nombre: string, cantidad: number, subtotal: number): void {
    this.ultimoAgregado.set({ nombre, cantidad, subtotal });
    clearTimeout(this.timerUltimoAgregado);
    this.timerUltimoAgregado = setTimeout(() => this.ultimoAgregado.set(null), DURACION_ULTIMO_AGREGADO_MS);
  }

  cambiarCantidad(item: ItemCarrito, delta: number): void {
    const nuevaCantidad = item.cantidad + delta;
    if (nuevaCantidad <= 0) {
      this.quitarDelCarrito(item, item.cantidad);
      return;
    }
    if (nuevaCantidad > item.producto.stockActual) return;

    this.carrito.set(
      this.carrito().map((i) => (i.producto.id === item.producto.id ? { ...i, cantidad: nuevaCantidad } : i)),
    );
  }

  private quitarDelCarrito(item: ItemCarrito, cantidadPrevia: number): void {
    this.carrito.set(this.carrito().filter((i) => i.producto.id !== item.producto.id));

    this.snackBar
      .open(`Quitaste ${item.producto.nombre} del carrito`, 'Deshacer', { duration: 4000 })
      .onAction()
      .subscribe(() => {
        this.carrito.set([...this.carrito(), { producto: item.producto, cantidad: cantidadPrevia }]);
      });
  }

  toggleCarrito(): void {
    this.carritoExpandido.set(!this.carritoExpandido());
  }

  abrirClientePicker(): void {
    this.dialog
      .open(ClientePickerDialog, {
        data: { nombre: this.nombreCliente(), telefono: this.telefonoCliente() },
        width: '420px',
        maxWidth: '95vw',
      })
      .afterClosed()
      .subscribe((resultado: ClientePickerResultado | null | undefined) => {
        if (resultado === undefined) return;
        if (resultado === null) {
          this.quitarCliente();
          return;
        }
        this.nombreCliente.set(resultado.nombre);
        this.telefonoCliente.set(resultado.telefono);
        this.clienteSeleccionado.set(resultado.cliente);
      });
  }

  quitarCliente(): void {
    this.nombreCliente.set('');
    this.telefonoCliente.set('');
    this.clienteSeleccionado.set(null);
  }

  cambiarMedioPago(valor: MedioPago): void {
    this.medioPago.set(valor);
    if (valor !== 'Efectivo') this.montoRecibido.set(null);
  }

  cobrar(): void {
    if (this.carrito().length === 0 || this.procesando() || this.faltaClienteParaACuenta()) return;

    this.procesando.set(true);
    this.ventasService
      .crear({
        items: this.carrito().map((i) => ({ productoId: i.producto.id, cantidad: i.cantidad })),
        medioPago: this.medioPago(),
        telefonoCliente: this.telefonoCliente().trim() || null,
        nombreCliente: this.nombreCliente().trim() || null,
      })
      .subscribe({
        next: (venta) => {
          this.procesando.set(false);
          this.carritoExpandido.set(false);
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
    this.carritoExpandido.set(false);
    this.nombreCliente.set('');
    this.telefonoCliente.set('');
    this.clienteSeleccionado.set(null);
    this.montoRecibido.set(null);
    this.medioPago.set('Efectivo');
    this.ventaResultado.set(null);
    this.busqueda.set('');
    // Refresca stock local para que la siguiente venta valide contra los números actuales.
    this.cargarProductos(false);
  }

  enviarPorWhatsApp(): void {
    const link = this.ventaResultado()?.whatsAppLink;
    if (link) window.open(link, '_blank');
  }
}
