import { NgTemplateOutlet } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatBottomSheet } from '@angular/material/bottom-sheet';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ItemCarrito } from '../../core/models/carrito.models';
import { Producto } from '../../core/models/catalog.models';
import { MedioPagoDto } from '../../core/models/configuracion.models';
import { FEATURE_CUENTA_CORRIENTE, MEDIO_PAGO_A_CUENTA } from '../../core/models/cuenta-corriente';
import { CajaDto, RankingProductoDto } from '../../core/models/reportes.models';
import { MedioPago, VentaDto } from '../../core/models/venta.models';
import { AuthService } from '../../core/services/auth.service';
import { CajaService } from '../../core/services/caja.service';
import { CarritoEnCursoService } from '../../core/services/carrito-en-curso.service';
import { CatalogoService } from '../../core/services/catalogo.service';
import { ConfiguracionService } from '../../core/services/configuracion.service';
import { ReportesService } from '../../core/services/reportes.service';
import { VentasService } from '../../core/services/ventas.service';
import { esMedida, formatearCantidad, formatearPesos, simboloUnidad } from '../../core/utils/cantidad.util';
import {
  baseEnCarrito,
  cantidadBaseDeLinea,
  cantidadParaResumen,
  esLineaMedida,
  esLineaSimple,
  mismaLinea,
  necesitaSelector,
  subtotalDeLinea,
  textoCantidadDeLinea,
} from '../../core/utils/carrito.util';
import { extraerMensajeError } from '../../core/utils/error.util';
import { ClientePickerDialog, ClientePickerResultado } from './cliente-picker-dialog/cliente-picker-dialog';
import { SelectorCantidad, SelectorCantidadData, SelectorCantidadResultado } from './selector-cantidad/selector-cantidad';

const TOP_MAS_VENDIDOS = 6;

interface NotificacionAgregado {
  id: number;
  nombre: string;
  cantidad: string;
  subtotal: number;
  saliendo: boolean;
}

const DURACION_NOTIFICACION_MS = 1200;
const DURACION_SALIDA_NOTIFICACION_MS = 200;

@Component({
  selector: 'app-venta',
  imports: [
    FormsModule,
    NgTemplateOutlet,
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
export class Venta implements OnInit {
  private readonly catalogoService = inject(CatalogoService);
  private readonly ventasService = inject(VentasService);
  private readonly reportesService = inject(ReportesService);
  private readonly cajaService = inject(CajaService);
  private readonly configuracionService = inject(ConfiguracionService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly bottomSheet = inject(MatBottomSheet);
  private readonly snackBar = inject(MatSnackBar);
  private readonly carritoEnCursoService = inject(CarritoEnCursoService);

  readonly medioPagoACuenta = MEDIO_PAGO_A_CUENTA;
  readonly tieneCuentaCorriente = this.authService.tieneFeature(FEATURE_CUENTA_CORRIENTE);

  private contadorNotificacion = 0;

  readonly cargando = signal(true);
  readonly cargandoCaja = signal(true);
  readonly cajaAbierta = signal(false);
  readonly productos = signal<Producto[]>([]);
  readonly mediosPago = signal<MedioPagoDto[]>([]);
  readonly rankingTop = signal<RankingProductoDto[]>([]);
  readonly busqueda = signal('');
  readonly carrito = this.carritoEnCursoService.items;
  readonly carritoExpandido = signal(false);
  readonly medioPago = this.carritoEnCursoService.medioPago;
  readonly nombreCliente = this.carritoEnCursoService.nombreCliente;
  readonly telefonoCliente = this.carritoEnCursoService.telefonoCliente;
  readonly clienteSeleccionado = this.carritoEnCursoService.clienteSeleccionado;
  readonly montoRecibido = this.carritoEnCursoService.montoRecibido;
  readonly procesando = signal(false);
  readonly ventaResultado = signal<VentaDto | null>(null);
  readonly notificaciones = signal<NotificacionAgregado[]>([]);

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
          (p.categoriaNombre?.toLowerCase().includes(termino) ?? false) ||
          (p.marcaNombre?.toLowerCase().includes(termino) ?? false),
      )
      .slice(0, 8);
  });

  readonly total = this.carritoEnCursoService.total;
  readonly cantidadItems = this.carritoEnCursoService.cantidadItems;

  readonly vuelto = computed(() => {
    const recibido = this.montoRecibido();
    return recibido == null ? null : recibido - this.total();
  });

  readonly esACuenta = computed(() => this.medioPago() === MEDIO_PAGO_A_CUENTA);
  readonly faltaClienteParaACuenta = computed(() => this.esACuenta() && !this.telefonoCliente().trim());
  readonly hayCliente = computed(() => !!this.nombreCliente().trim() || !!this.telefonoCliente().trim());

  /** Para la insignia de la tarjeta: cuánto lleva de ese producto (los medidos cuentan como una línea). */
  cantidadEnCarrito(productoId: string): number {
    return cantidadParaResumen(this.carrito().filter((i) => i.producto.id === productoId));
  }

  // --- Cómo se muestra cada producto y cada línea ---

  precioTexto(producto: Producto): string {
    return `$${producto.precio}${esMedida(producto.unidad) ? ' / ' + simboloUnidad(producto.unidad) : ''}`;
  }

  stockTexto(producto: Producto): string {
    const cantidad = formatearCantidad(producto.stockActual, producto.unidad);
    const sinUnidad = producto.unidad === 'Unidad' ? String(producto.stockActual) : cantidad;
    return producto.enStockMinimo ? `Quedan ${sinUnidad}` : `Stock: ${sinUnidad}`;
  }

  subtotal(item: ItemCarrito): number {
    return subtotalDeLinea(item);
  }

  detalleLinea(item: ItemCarrito): string {
    return `${textoCantidadDeLinea(item)} × ${formatearPesos(item.presentacion ? item.presentacion.precio : item.producto.precio)}`;
  }

  esSimple(item: ItemCarrito): boolean {
    return esLineaSimple(item);
  }

  esMedida(item: ItemCarrito): boolean {
    return esLineaMedida(item);
  }

  nombreLinea(item: ItemCarrito): string {
    return item.presentacion ? `${item.producto.nombre} · ${item.presentacion.nombre}` : item.producto.nombre;
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
    // Un producto medido o con presentaciones pide primero cuánto se lleva; el resto se suma de a uno, como siempre.
    if (necesitaSelector(producto)) {
      this.abrirSelector(producto, null);
      return;
    }

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

    this.carritoEnCursoService.tocar();
    this.mostrarNotificacion(producto.nombre, String(nuevaCantidad), producto.precio * nuevaCantidad);
  }

  /** Abre la hoja para elegir peso, volumen o presentación; con una línea existente la edita en lugar de sumar otra. */
  abrirSelector(producto: Producto, edicion: ItemCarrito | null): void {
    const data: SelectorCantidadData = {
      producto,
      stockDisponible: Math.max(0, Math.round((producto.stockActual - baseEnCarrito(this.carrito(), producto.id, edicion)) * 1000) / 1000),
      edicion,
    };

    this.bottomSheet
      .open<SelectorCantidad, SelectorCantidadData, SelectorCantidadResultado | undefined>(SelectorCantidad, { data, panelClass: 'selector-cantidad-hoja' })
      .afterDismissed()
      .subscribe((resultado) => {
        if (!resultado) return;
        this.aplicarSeleccion(producto, edicion, resultado);
      });
  }

  private aplicarSeleccion(producto: Producto, edicion: ItemCarrito | null, resultado: SelectorCantidadResultado): void {
    const nueva: ItemCarrito = { producto, cantidad: resultado.cantidad, presentacion: resultado.presentacion };
    const resto = edicion ? this.carrito().filter((i) => i !== edicion) : this.carrito();
    const repetida = resto.find((i) => mismaLinea(i, nueva));

    let linea: ItemCarrito;
    if (repetida) {
      linea = { ...repetida, cantidad: Math.round((repetida.cantidad + nueva.cantidad) * 1000) / 1000 };
      this.carrito.set(resto.map((i) => (i === repetida ? linea : i)));
    } else {
      linea = nueva;
      this.carrito.set(edicion ? this.carrito().map((i) => (i === edicion ? nueva : i)) : [...resto, nueva]);
    }

    this.carritoEnCursoService.tocar();
    this.mostrarNotificacion(this.nombreLinea(linea), textoCantidadDeLinea(linea), subtotalDeLinea(linea));
  }

  editarLinea(item: ItemCarrito): void {
    this.abrirSelector(item.producto, item);
  }

  // Notificación breve arriba a la derecha ("Coca Cola · Cantidad: 3 · $6300") para que el
  // vendedor sepa qué cargó sin tener que abrir el carrito. Se apilan si se agregan varias
  // seguidas (la más nueva arriba) y cada una desaparece sola a los DURACION_NOTIFICACION_MS,
  // sin afectar a las demás.
  private mostrarNotificacion(nombre: string, cantidad: string, subtotal: number): void {
    const id = ++this.contadorNotificacion;
    this.notificaciones.update((actual) => [...actual, { id, nombre, cantidad, subtotal, saliendo: false }]);

    setTimeout(() => {
      this.notificaciones.update((actual) => actual.map((n) => (n.id === id ? { ...n, saliendo: true } : n)));
    }, DURACION_NOTIFICACION_MS - DURACION_SALIDA_NOTIFICACION_MS);

    setTimeout(() => {
      this.notificaciones.update((actual) => actual.filter((n) => n.id !== id));
    }, DURACION_NOTIFICACION_MS);
  }

  cambiarCantidad(item: ItemCarrito, delta: number): void {
    const nuevaCantidad = item.cantidad + delta;
    if (nuevaCantidad <= 0) {
      this.quitarDelCarrito(item);
      return;
    }

    // El stock se controla sumando todas las líneas del producto: suelto y bolsas salen del mismo stock.
    const nueva = { ...item, cantidad: nuevaCantidad };
    const base = baseEnCarrito(this.carrito(), item.producto.id, item) + cantidadBaseDeLinea(nueva);
    if (base > item.producto.stockActual + 1e-9) return;

    this.carrito.set(this.carrito().map((i) => (mismaLinea(i, item) ? nueva : i)));
    this.carritoEnCursoService.tocar();
  }

  quitarDelCarrito(item: ItemCarrito): void {
    this.carrito.set(this.carrito().filter((i) => !mismaLinea(i, item)));
    this.carritoEnCursoService.tocar();

    this.snackBar
      .open(`Quitaste ${this.nombreLinea(item)} del carrito`, 'Deshacer', { duration: 4000 })
      .onAction()
      .subscribe(() => {
        this.carrito.set([...this.carrito(), item]);
        this.carritoEnCursoService.tocar();
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
        this.carritoEnCursoService.tocar();
      });
  }

  quitarCliente(): void {
    this.nombreCliente.set('');
    this.telefonoCliente.set('');
    this.clienteSeleccionado.set(null);
    this.carritoEnCursoService.tocar();
  }

  cambiarMedioPago(valor: MedioPago): void {
    this.medioPago.set(valor);
    if (valor !== 'Efectivo') this.montoRecibido.set(null);
    this.carritoEnCursoService.tocar();
  }

  cobrar(): void {
    if (this.carrito().length === 0 || this.procesando() || this.faltaClienteParaACuenta()) return;

    this.procesando.set(true);
    this.ventasService
      .crear({
        items: this.carrito().map((i) => ({ productoId: i.producto.id, cantidad: i.cantidad, presentacionId: i.presentacion?.id ?? null })),
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
    this.carritoEnCursoService.cancelar();
    this.carritoExpandido.set(false);
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
