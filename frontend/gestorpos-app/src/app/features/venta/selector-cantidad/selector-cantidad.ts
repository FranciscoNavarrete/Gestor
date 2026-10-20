import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatBottomSheetModule, MatBottomSheetRef, MAT_BOTTOM_SHEET_DATA } from '@angular/material/bottom-sheet';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ItemCarrito } from '../../../core/models/carrito.models';
import { Presentacion, Producto } from '../../../core/models/catalog.models';
import {
  esMedida,
  formatearCantidad,
  formatearNumero,
  formatearPesos,
  simboloUnidad,
  tieneMasDeTresDecimales,
} from '../../../core/utils/cantidad.util';

export interface SelectorCantidadData {
  producto: Producto;
  /** Stock que queda para esta línea: el del producto menos lo que ya llevan otras líneas del carrito. */
  stockDisponible: number;
  /** Línea que se está editando, o null si se agrega una nueva. */
  edicion: ItemCarrito | null;
}

export interface SelectorCantidadResultado {
  presentacion: Presentacion | null;
  cantidad: number;
}

interface Subunidad {
  simbolo: string;
  /** Cuánto vale en la unidad del producto: 1 g = 0,001 kg. */
  factor: number;
}

function redondear(valor: number, decimales: number): number {
  const f = 10 ** decimales;
  return Math.round((valor + Number.EPSILON) * f) / f;
}

/**
 * Hoja inferior para elegir cuánto se lleva: peso o volumen suelto (atajos desde 100 g, o tipeando en gramos y
 * kilos), o una presentación ("2 bolsas de 15 kg"). Calcula el importe y avisa si supera el stock.
 */
@Component({
  selector: 'app-selector-cantidad',
  imports: [FormsModule, MatBottomSheetModule, MatButtonModule, MatButtonToggleModule, MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './selector-cantidad.html',
  styleUrl: './selector-cantidad.scss',
})
export class SelectorCantidad {
  private readonly ref = inject(MatBottomSheetRef<SelectorCantidad, SelectorCantidadResultado>);
  readonly data = inject<SelectorCantidadData>(MAT_BOTTOM_SHEET_DATA);

  readonly producto = this.data.producto;
  readonly unidad = this.producto.unidad;
  readonly medida = esMedida(this.unidad);
  readonly simbolo = simboloUnidad(this.unidad);
  readonly esEdicion = !!this.data.edicion;

  readonly subunidades: Subunidad[] =
    this.unidad === 'Kilo'
      ? [{ simbolo: 'g', factor: 0.001 }, { simbolo: 'kg', factor: 1 }]
      : this.unidad === 'Litro'
        ? [{ simbolo: 'ml', factor: 0.001 }, { simbolo: 'L', factor: 1 }]
        : [{ simbolo: 'm', factor: 1 }];

  /** Atajos de cantidades habituales, ya en la unidad en la que se escriben. */
  readonly atajos: { texto: string; valor: number; subunidad: string }[] =
    this.unidad === 'Kilo'
      ? [{ texto: '100 g', valor: 100, subunidad: 'g' }, { texto: '250 g', valor: 250, subunidad: 'g' }, { texto: '500 g', valor: 500, subunidad: 'g' }, { texto: '1 kg', valor: 1, subunidad: 'kg' }]
      : this.unidad === 'Litro'
        ? [{ texto: '100 ml', valor: 100, subunidad: 'ml' }, { texto: '250 ml', valor: 250, subunidad: 'ml' }, { texto: '500 ml', valor: 500, subunidad: 'ml' }, { texto: '1 L', valor: 1, subunidad: 'L' }]
        : this.unidad === 'Metro'
          ? [{ texto: '0,5 m', valor: 0.5, subunidad: 'm' }, { texto: '1 m', valor: 1, subunidad: 'm' }, { texto: '2 m', valor: 2, subunidad: 'm' }]
          : [];

  readonly presentacion = signal<Presentacion | null>(this.data.edicion?.presentacion ?? null);
  readonly subunidad = signal<string>(this.subunidadInicial());
  readonly texto = signal<string>(this.textoInicial());
  readonly enteros = signal<number>(this.enterosIniciales());

  readonly opciones = [
    { presentacion: null as Presentacion | null, nombre: this.unidad === 'Unidad' ? 'Unidad' : 'Suelto' },
    ...this.producto.presentaciones.map((p) => ({ presentacion: p as Presentacion | null, nombre: p.nombre })),
  ];

  readonly sueltoMedido = computed(() => this.medida && this.presentacion() === null);

  readonly precio = computed(() => this.presentacion()?.precio ?? this.producto.precio);
  readonly textoPrecio = computed(() => {
    const p = this.presentacion();
    if (p) return formatearPesos(p.precio);
    return `${formatearPesos(this.producto.precio)}${this.medida ? ' / ' + this.simbolo : ''}`;
  });

  /** Cantidad de la línea: kilos, litros o metros sueltos; o cuántas presentaciones / unidades. */
  readonly cantidad = computed(() => {
    if (!this.sueltoMedido()) return this.enteros();
    const valor = Number(this.texto().trim().replace(',', '.'));
    const factor = this.subunidades.find((s) => s.simbolo === this.subunidad())?.factor ?? 1;
    return Number.isFinite(valor) ? redondear(valor * factor, 6) : NaN;
  });

  readonly importe = computed(() => {
    const c = this.cantidad();
    // Con más decimales de los permitidos no se muestra un importe a medias: primero hay que corregir la cantidad.
    if (this.sueltoMedido() && Number.isFinite(c) && tieneMasDeTresDecimales(c)) return 0;
    return Number.isFinite(c) ? redondear(this.precio() * c, 2) : 0;
  });

  /** Lo que se descuenta del stock del producto. */
  readonly descuenta = computed(() => {
    const c = this.cantidad();
    return Number.isFinite(c) ? redondear(c * (this.presentacion()?.cantidadBase ?? 1), 3) : 0;
  });

  readonly error = computed<string | null>(() => {
    const c = this.cantidad();
    if (!Number.isFinite(c) || c <= 0) return this.sueltoMedido() && this.texto().trim() ? 'Escribí una cantidad mayor a cero.' : null;
    if (this.sueltoMedido() && tieneMasDeTresDecimales(c)) return `La cantidad admite hasta 3 decimales en ${this.simbolo}.`;
    if (this.descuenta() > this.data.stockDisponible + 1e-9) {
      return `Supera el stock disponible: ${formatearCantidad(this.data.stockDisponible, this.unidad)}.`;
    }
    return null;
  });

  readonly valido = computed(() => {
    const c = this.cantidad();
    return Number.isFinite(c) && c > 0 && this.error() === null;
  });

  readonly textoStock = computed(() => {
    if (!this.valido()) return `Stock disponible: ${formatearCantidad(this.data.stockDisponible, this.unidad)}`;
    const queda = redondear(this.data.stockDisponible - this.descuenta(), 3);
    const base = `Descuenta ${formatearCantidad(this.descuenta(), this.unidad)} del stock`;
    return `${base} · quedan ${formatearCantidad(queda, this.unidad)}`;
  });

  readonly etiquetaSubunidad = (s: Subunidad) => s.simbolo;

  elegirOpcion(presentacion: Presentacion | null): void {
    this.presentacion.set(presentacion);
    if (presentacion || !this.medida) this.enteros.update((n) => (n >= 1 ? Math.round(n) : 1));
  }

  elegirAtajo(a: { valor: number; subunidad: string }): void {
    this.subunidad.set(a.subunidad);
    this.texto.set(formatearNumero(a.valor));
  }

  cambiarSubunidad(simbolo: string): void {
    // Al pasar de gramos a kilos (o al revés) se convierte lo escrito, así 500 g pasa a 0,5 kg.
    const actual = this.subunidades.find((s) => s.simbolo === this.subunidad());
    const nueva = this.subunidades.find((s) => s.simbolo === simbolo);
    const c = this.cantidad();
    this.subunidad.set(simbolo);
    if (actual && nueva && Number.isFinite(c) && c > 0) this.texto.set(formatearNumero(redondear(c / nueva.factor, 3)));
  }

  sumar(delta: number): void {
    this.enteros.update((n) => Math.max(1, n + delta));
  }

  agregar(): void {
    if (!this.valido()) return;
    this.ref.dismiss({ presentacion: this.presentacion(), cantidad: this.cantidad() } satisfies SelectorCantidadResultado);
  }

  cancelar(): void {
    this.ref.dismiss(undefined);
  }

  textoImporte(): string {
    return formatearPesos(this.importe());
  }

  private subunidadInicial(): string {
    const e = this.data.edicion;
    if (e && !e.presentacion && e.cantidad >= 1 && this.subunidades.length > 1) return this.subunidades[1].simbolo;
    return this.subunidades[0].simbolo;
  }

  private textoInicial(): string {
    const e = this.data.edicion;
    if (!e || e.presentacion || !this.medida) return '';
    const sub = this.subunidades.find((s) => s.simbolo === this.subunidadInicial());
    return formatearNumero(redondear(e.cantidad / (sub?.factor ?? 1), 3));
  }

  private enterosIniciales(): number {
    const e = this.data.edicion;
    return e && (e.presentacion || !this.medida) ? e.cantidad : 1;
  }
}
