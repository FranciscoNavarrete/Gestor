import { Component, Input, OnChanges, OnInit, SimpleChanges, inject, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Presentacion, UnidadMedida } from '../../../core/models/catalog.models';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { formatearCantidad, formatearPesos, simboloUnidad, tieneMasDeTresDecimales } from '../../../core/utils/cantidad.util';
import { extraerMensajeError } from '../../../core/utils/error.util';
import { gananciaDesdePrecio, redondearImporte } from '../../../core/utils/precio.util';
import { CalculadoraPrecio } from '../calculadora-precio/calculadora-precio';

/** Presentación tal como se edita: en un producto nuevo todavía no tiene id. */
export interface PresentacionEditable {
  id?: string;
  nombre: string;
  cantidadBase: number;
  precio: number;
}

/**
 * Lista de presentaciones de un producto ("Bolsa 15 kg", "Caja x12") con el formulario para agregar y editar.
 * En un producto nuevo (sin productoId) las presentaciones quedan en memoria y el diálogo las manda al crear el
 * producto. En uno existente cada cambio se guarda al instante contra la API.
 */
@Component({
  selector: 'app-presentaciones-editor',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    CalculadoraPrecio,
  ],
  templateUrl: './presentaciones-editor.html',
  styleUrl: './presentaciones-editor.scss',
})
export class PresentacionesEditor implements OnInit, OnChanges {
  private readonly fb = inject(FormBuilder);
  private readonly catalogoService = inject(CatalogoService);

  /** null = producto nuevo, todavía sin guardar. */
  @Input() productoId: string | null = null;
  @Input() unidad: UnidadMedida = 'Unidad';
  /** Costo por unidad del producto (por kg, por litro...); sirve para proponer el costo de cada presentación. */
  @Input() costoBase: number | null = null;
  /** Falso si la función está apagada: se pueden editar y quitar las que hay, pero no agregar. */
  @Input() puedeAgregar = true;
  @Input() iniciales: Presentacion[] = [];

  /** Avisa que se guardó algo contra la API, para que la lista de productos se actualice. */
  readonly guardoCambios = output<void>();

  readonly lista = signal<PresentacionEditable[]>([]);
  readonly formAbierto = signal(false);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  private editandoIndice: number | null = null;

  readonly form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(100)]],
    cantidadBase: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.001)]),
    costo: this.fb.control<number | null>(null, [Validators.min(0)]),
    precio: this.fb.control<number | null>(null, [Validators.required, Validators.min(0)]),
  });

  ngOnInit(): void {
    this.lista.set(this.iniciales.map((p) => ({ id: p.id, nombre: p.nombre, cantidadBase: p.cantidadBase, precio: p.precio })));
    this.aplicarValidadores();

    // El costo de la presentación se propone solo (cantidad × costo por unidad) mientras no se lo corrija a mano.
    this.form.controls.cantidadBase.valueChanges.subscribe(() => this.proponerCosto());
  }

  ngOnChanges(cambios: SimpleChanges): void {
    if (cambios['unidad'] && !cambios['unidad'].firstChange) this.aplicarValidadores();
    if (cambios['costoBase'] && !cambios['costoBase'].firstChange) this.proponerCosto();
  }

  get simbolo(): string {
    return simboloUnidad(this.unidad);
  }

  get hintCantidad(): string {
    return this.unidad === 'Unidad' ? 'Cuántas unidades trae. Ej: 12 para una caja' : `Hasta 3 decimales. Ej: 15 para una bolsa de 15 ${this.simbolo}`;
  }

  textoCantidad(p: PresentacionEditable): string {
    return `= ${formatearCantidad(p.cantidadBase, this.unidad)}`;
  }

  textoPrecio(p: PresentacionEditable): string {
    return formatearPesos(p.precio);
  }

  /** Cuánto equivale por unidad del producto; solo tiene sentido en productos medidos. */
  equivalencia(p: PresentacionEditable): string | null {
    if (this.unidad === 'Unidad' || p.cantidadBase <= 0) return null;
    return `${formatearPesos(redondearImporte(p.precio / p.cantidadBase))} por ${this.simbolo}`;
  }

  gananciaTexto(p: PresentacionEditable): string | null {
    if (this.costoBase === null || this.costoBase <= 0) return null;
    const porcentaje = gananciaDesdePrecio(redondearImporte(this.costoBase * p.cantidadBase), p.precio);
    return porcentaje === null ? null : `ganancia ${String(porcentaje).replace('.', ',')}%`;
  }

  abrirNueva(): void {
    this.editandoIndice = null;
    this.error.set(null);
    this.form.reset({ nombre: '', cantidadBase: null, costo: null, precio: null });
    this.formAbierto.set(true);
  }

  abrirEdicion(indice: number): void {
    const p = this.lista()[indice];
    this.editandoIndice = indice;
    this.error.set(null);
    this.form.reset({ nombre: p.nombre, cantidadBase: p.cantidadBase, costo: null, precio: p.precio });
    this.proponerCosto();
    this.formAbierto.set(true);
  }

  cancelar(): void {
    this.formAbierto.set(false);
    this.error.set(null);
  }

  guardar(): void {
    if (this.form.invalid || this.guardando()) return;
    const v = this.form.getRawValue();
    const pedida: PresentacionEditable = { nombre: v.nombre.trim(), cantidadBase: v.cantidadBase!, precio: v.precio! };
    const indice = this.editandoIndice;

    if (this.productoId === null) {
      const nombreRepetido = this.lista().some((p, i) => i !== indice && p.nombre.toLowerCase() === pedida.nombre.toLowerCase());
      if (nombreRepetido) {
        this.error.set(`Ya hay una presentación llamada '${pedida.nombre}'.`);
        return;
      }
      this.lista.update((actual) => (indice === null ? [...actual, pedida] : actual.map((p, i) => (i === indice ? pedida : p))));
      this.formAbierto.set(false);
      return;
    }

    this.guardando.set(true);
    this.error.set(null);
    const existente = indice === null ? null : this.lista()[indice];
    const request$ = existente?.id
      ? this.catalogoService.editarPresentacion(this.productoId, existente.id, pedida)
      : this.catalogoService.crearPresentacion(this.productoId, pedida);

    request$.subscribe({
      next: (guardada) => {
        const nueva: PresentacionEditable = { id: guardada.id, nombre: guardada.nombre, cantidadBase: guardada.cantidadBase, precio: guardada.precio };
        this.lista.update((actual) => (indice === null ? [...actual, nueva] : actual.map((p, i) => (i === indice ? nueva : p))));
        this.guardando.set(false);
        this.formAbierto.set(false);
        this.guardoCambios.emit();
      },
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }

  quitar(indice: number): void {
    const p = this.lista()[indice];
    if (!confirm(`¿Quitar la presentación "${p.nombre}"? Las ventas anteriores no cambian.`)) return;

    if (this.productoId === null || !p.id) {
      this.lista.update((actual) => actual.filter((_, i) => i !== indice));
      return;
    }

    this.catalogoService.desactivarPresentacion(this.productoId, p.id).subscribe({
      next: () => {
        this.lista.update((actual) => actual.filter((_, i) => i !== indice));
        this.guardoCambios.emit();
      },
      error: (err) => this.error.set(extraerMensajeError(err)),
    });
  }

  private proponerCosto(): void {
    const control = this.form.controls.costo;
    if (control.dirty) return;
    const cantidad = this.form.controls.cantidadBase.value;
    if (this.costoBase === null || this.costoBase <= 0 || cantidad === null || cantidad <= 0) return;
    control.setValue(redondearImporte(this.costoBase * cantidad));
  }

  private aplicarValidadores(): void {
    const reglas: ValidatorFn[] = [Validators.required, Validators.min(0.001)];
    if (this.unidad === 'Unidad') reglas.push(Validators.pattern(/^\d+$/));
    else reglas.push((c) => (c.value !== null && c.value !== '' && tieneMasDeTresDecimales(Number(c.value)) ? { decimales: true } : null));
    this.form.controls.cantidadBase.setValidators(reglas);
    this.form.controls.cantidadBase.updateValueAndValidity({ emitEvent: false });
  }
}
