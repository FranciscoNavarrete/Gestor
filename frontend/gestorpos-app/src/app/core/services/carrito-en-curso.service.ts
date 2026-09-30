import { Injectable, computed, inject, signal } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ItemCarrito } from '../models/carrito.models';
import { Cliente } from '../models/cliente.models';
import { MedioPago } from '../models/venta.models';

const EXPIRA_MS = 60 * 60 * 1000;
const AVISO_ANTES_MS = 5 * 60 * 1000;
const INTERVALO_REVISION_MS = 30 * 1000;

/** Estado de la venta que se está armando, compartido fuera del componente Venta para que
 * sobreviva la navegación a otras secciones (Productos, Caja, etc.) sin perderse. */
@Injectable({ providedIn: 'root' })
export class CarritoEnCursoService {
  private readonly snackBar = inject(MatSnackBar);

  readonly items = signal<ItemCarrito[]>([]);
  readonly nombreCliente = signal('');
  readonly telefonoCliente = signal('');
  readonly clienteSeleccionado = signal<Cliente | null>(null);
  readonly medioPago = signal<MedioPago>('Efectivo');
  readonly montoRecibido = signal<number | null>(null);

  private readonly ultimaActividad = signal(Date.now());
  private avisoMostrado = false;

  readonly total = computed(() => this.items().reduce((acc, i) => acc + i.producto.precio * i.cantidad, 0));
  readonly cantidadItems = computed(() => this.items().reduce((acc, i) => acc + i.cantidad, 0));
  readonly hayVentaEnCurso = computed(() => this.items().length > 0);

  constructor() {
    setInterval(() => this.revisarExpiracion(), INTERVALO_REVISION_MS);
  }

  tocar(): void {
    this.ultimaActividad.set(Date.now());
    this.avisoMostrado = false;
  }

  cancelar(): void {
    this.items.set([]);
    this.nombreCliente.set('');
    this.telefonoCliente.set('');
    this.clienteSeleccionado.set(null);
    this.medioPago.set('Efectivo');
    this.montoRecibido.set(null);
    this.avisoMostrado = false;
  }

  private revisarExpiracion(): void {
    if (this.items().length === 0) return;

    const transcurrido = Date.now() - this.ultimaActividad();
    if (transcurrido >= EXPIRA_MS) {
      this.cancelar();
      this.snackBar.open('Se canceló tu venta en curso por inactividad.', 'Cerrar', { duration: 6000 });
      return;
    }

    if (transcurrido >= EXPIRA_MS - AVISO_ANTES_MS && !this.avisoMostrado) {
      this.avisoMostrado = true;
      this.snackBar
        .open('Tu venta en curso se cancela en 5 minutos por inactividad.', 'Seguir vendiendo', { duration: 10000 })
        .onAction()
        .subscribe(() => this.tocar());
    }
  }
}
