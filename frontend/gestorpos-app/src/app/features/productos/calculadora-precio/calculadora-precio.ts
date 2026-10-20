import { Component, Input, OnDestroy, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Subscription } from 'rxjs';
import { formatearPesos } from '../../../core/utils/cantidad.util';
import {
  gananciaDesdePrecio,
  precioDesdeGanancia,
  redondearImporte,
  redondearParaCobrar,
} from '../../../core/utils/precio.util';

type Ultimo = 'ganancia' | 'precio';

/**
 * Ayuda para poner precios: Costo, Ganancia % (sobre el costo) y Precio se calculan entre sí. Manda lo último
 * que se tocó: si se escribe el % se calcula el precio; si se escribe el precio se calcula el %. El % no se
 * guarda, es solo una ayuda; lo que queda guardado son los controles de costo y precio del formulario.
 */
@Component({
  selector: 'app-calculadora-precio',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './calculadora-precio.html',
  styleUrl: './calculadora-precio.scss',
})
export class CalculadoraPrecio implements OnInit, OnDestroy {
  @Input({ required: true }) costo!: FormControl<number | null>;
  @Input({ required: true }) precio!: FormControl<number | null>;
  /** Texto después de los importes, por ejemplo "por kg". */
  @Input() porUnidad = '';
  @Input() etiquetaCosto = 'Costo';
  @Input() etiquetaPrecio = 'Precio de venta';

  readonly ganancia = new FormControl<number | null>(null, [Validators.min(-100)]);
  /** Se incrementa en cada cambio para que el resumen se vuelva a calcular. */
  readonly version = signal(0);

  private ultimo: Ultimo = 'precio';
  private readonly suscripciones = new Subscription();

  ngOnInit(): void {
    this.recalcularGananciaDesdePrecio();

    this.suscripciones.add(
      this.costo.valueChanges.subscribe(() => {
        if (this.ultimo === 'ganancia') this.recalcularPrecioDesdeGanancia();
        else this.recalcularGananciaDesdePrecio();
        this.version.update((v) => v + 1);
      }),
    );
    this.suscripciones.add(
      this.ganancia.valueChanges.subscribe(() => {
        this.ultimo = 'ganancia';
        this.recalcularPrecioDesdeGanancia();
        this.version.update((v) => v + 1);
      }),
    );
    this.suscripciones.add(
      this.precio.valueChanges.subscribe(() => {
        this.ultimo = 'precio';
        this.recalcularGananciaDesdePrecio();
        this.version.update((v) => v + 1);
      }),
    );
  }

  ngOnDestroy(): void {
    this.suscripciones.unsubscribe();
  }

  resumen(): string {
    this.version();
    const costo = this.costo.value;
    const precio = this.precio.value;
    if (precio === null || costo === null || costo <= 0) return '';
    const diferencia = redondearImporte(precio - costo);
    const sufijo = this.porUnidad ? ` ${this.porUnidad}` : '';
    const porcentaje = gananciaDesdePrecio(costo, precio);
    if (diferencia < 0)
      return `Vendés por debajo del costo: perdés ${formatearPesos(Math.abs(diferencia))}${sufijo}`;
    if (diferencia === 0) return 'Vendés al costo: no ganás nada';
    return `Ganás ${formatearPesos(diferencia)}${sufijo}, ${this.textoPorcentaje(porcentaje)}% sobre el costo`;
  }

  hayPerdida(): boolean {
    this.version();
    const costo = this.costo.value;
    const precio = this.precio.value;
    return costo !== null && precio !== null && costo > 0 && precio < costo;
  }

  precioRedondeado(): number | null {
    this.version();
    const precio = this.precio.value;
    if (precio === null || precio <= 0) return null;
    const redondeado = redondearParaCobrar(precio);
    return redondeado !== precio && redondeado > 0 ? redondeado : null;
  }

  redondear(): void {
    const redondeado = this.precioRedondeado();
    if (redondeado === null) return;
    this.precio.setValue(redondeado);
    this.precio.markAsDirty();
  }

  private textoPorcentaje(valor: number | null): string {
    return valor === null ? '0' : String(valor).replace('.', ',');
  }

  private recalcularPrecioDesdeGanancia(): void {
    const costo = this.costo.value;
    const porcentaje = this.ganancia.value;
    if (costo === null || costo <= 0 || porcentaje === null) return;
    this.precio.setValue(precioDesdeGanancia(costo, porcentaje), { emitEvent: false });
    this.precio.markAsDirty();
  }

  private recalcularGananciaDesdePrecio(): void {
    const costo = this.costo.value;
    const precio = this.precio.value;
    const porcentaje = costo !== null && precio !== null ? gananciaDesdePrecio(costo, precio) : null;
    this.ganancia.setValue(porcentaje, { emitEvent: false });
  }
}
