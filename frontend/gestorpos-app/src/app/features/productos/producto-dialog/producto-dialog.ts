import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { startWith } from 'rxjs';
import { Categoria, Marca, Producto, UnidadMedida } from '../../../core/models/catalog.models';
import { FEATURE_MEDIDAS } from '../../../core/models/medidas-feature';
import { AuthService } from '../../../core/services/auth.service';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { UNIDADES, esMedida, formatearPesos, simboloUnidad, tieneMasDeTresDecimales } from '../../../core/utils/cantidad.util';
import { extraerMensajeError } from '../../../core/utils/error.util';
import { redondearImporte } from '../../../core/utils/precio.util';
import { CategoriaDialog } from '../categoria-dialog/categoria-dialog';
import { MarcaDialog } from '../marca-dialog/marca-dialog';
import { CalculadoraPrecio } from '../calculadora-precio/calculadora-precio';
import { PresentacionesEditor } from '../presentaciones-editor/presentaciones-editor';

export interface ProductoDialogData {
  producto: Producto | null;
  categorias: Categoria[];
  marcas: Marca[];
}

@Component({
  selector: 'app-producto-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    CalculadoraPrecio,
    PresentacionesEditor,
  ],
  templateUrl: './producto-dialog.html',
  styleUrl: './producto-dialog.scss',
})
export class ProductoDialog {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly catalogoService = inject(CatalogoService);
  private readonly dialog = inject(MatDialog);
  private readonly dialogRef = inject(MatDialogRef<ProductoDialog>);
  readonly data = inject<ProductoDialogData>(MAT_DIALOG_DATA);

  readonly esEdicion = this.data.producto !== null;
  /** La función "Medidas y presentaciones" la prende el operador por negocio. */
  readonly tieneMedidas = this.authService.tieneFeature(FEATURE_MEDIDAS);
  readonly unidades = UNIDADES;
  readonly unidad = signal<UnidadMedida>(this.data.producto?.unidad ?? 'Unidad');
  readonly medida = computed(() => esMedida(this.unidad()));
  readonly simbolo = computed(() => simboloUnidad(this.unidad()));
  /** Un producto que ya tiene presentaciones las sigue mostrando aunque después se apague la función. */
  readonly mostrarPresentaciones = this.tieneMedidas || (this.data.producto?.presentaciones.length ?? 0) > 0;

  readonly categorias = signal(this.data.categorias);
  readonly marcas = signal(this.data.marcas);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  private huboCambiosEnPresentaciones = false;

  readonly editor = viewChild(PresentacionesEditor);

  readonly form = this.fb.nonNullable.group({
    sku: [this.data.producto?.sku ?? '', [Validators.required]],
    nombre: [this.data.producto?.nombre ?? '', [Validators.required]],
    categoriaId: this.fb.control<string | null>(this.data.producto?.categoriaId ?? null),
    marcaId: this.fb.control<string | null>(this.data.producto?.marcaId ?? null),
    precio: this.fb.control<number | null>(this.data.producto?.precio ?? null, [Validators.required, Validators.min(0)]),
    costo: this.fb.control<number | null>(this.data.producto?.costo ?? null, [Validators.required, Validators.min(0)]),
    stockActual: this.fb.control<number | null>(this.data.producto?.stockActual ?? null, [Validators.required, Validators.min(0)]),
    stockMinimo: this.fb.control<number | null>(this.data.producto?.stockMinimo ?? null, [Validators.required, Validators.min(0)]),
  });

  /** Costo por unidad, para que cada presentación proponga su costo (cantidad × costo por kg). */
  readonly costoActual = toSignal(this.form.controls.costo.valueChanges.pipe(startWith(this.form.controls.costo.value)), {
    initialValue: this.form.controls.costo.value,
  });

  constructor() {
    this.aplicarValidadoresDeStock();
  }

  elegirUnidad(unidad: UnidadMedida): void {
    this.unidad.set(unidad);
    this.aplicarValidadoresDeStock();
  }

  /** Atajos para el suelto: "Igual que Bolsa 15 kg" completa el precio con lo que sale el kilo en esa bolsa. */
  atajosIgualQuePresentacion(): { texto: string; precioPorUnidad: number }[] {
    if (!this.medida()) return [];
    const lista = this.editor()?.lista() ?? [];
    return lista
      .filter((p) => p.cantidadBase > 0)
      .map((p) => {
        const precioPorUnidad = redondearImporte(p.precio / p.cantidadBase);
        return { texto: `Igual que ${p.nombre} (${formatearPesos(precioPorUnidad)} por ${this.simbolo()})`, precioPorUnidad };
      });
  }

  usarPrecio(precio: number): void {
    this.form.controls.precio.setValue(precio);
    this.form.controls.precio.markAsDirty();
  }

  nuevaCategoria(): void {
    this.dialog
      .open(CategoriaDialog)
      .afterClosed()
      .subscribe((categoria: Categoria | undefined) => {
        if (!categoria) return;
        this.categorias.set([...this.categorias(), categoria]);
        this.form.patchValue({ categoriaId: categoria.id });
      });
  }

  nuevaMarca(): void {
    this.dialog
      .open(MarcaDialog)
      .afterClosed()
      .subscribe((marca: Marca | undefined) => {
        if (!marca) return;
        this.marcas.set([...this.marcas(), marca]);
        this.form.patchValue({ marcaId: marca.id });
      });
  }

  presentacionesGuardadas(): void {
    this.huboCambiosEnPresentaciones = true;
  }

  /** Si se agregaron o quitaron presentaciones, la lista de productos tiene que actualizarse al cerrar. */
  cerrar(): void {
    this.dialogRef.close(this.huboCambiosEnPresentaciones ? true : undefined);
  }

  guardar(): void {
    if (this.form.invalid || this.guardando()) return;

    this.guardando.set(true);
    this.error.set(null);
    const valores = this.form.getRawValue();

    const request$ = this.esEdicion
      ? this.catalogoService.editarProducto(this.data.producto!.id, {
          nombre: valores.nombre,
          categoriaId: valores.categoriaId,
          marcaId: valores.marcaId,
          precio: valores.precio!,
          costo: valores.costo!,
          stockMinimo: valores.stockMinimo!,
        })
      : this.catalogoService.crearProducto({
          sku: valores.sku,
          nombre: valores.nombre,
          categoriaId: valores.categoriaId,
          marcaId: valores.marcaId,
          precio: valores.precio!,
          costo: valores.costo!,
          stockActual: valores.stockActual!,
          stockMinimo: valores.stockMinimo!,
          ...this.datosDeMedidas(),
        });

    request$.subscribe({
      next: (producto) => this.dialogRef.close(producto),
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }

  /** Solo se mandan unidad y presentaciones si el negocio tiene la función: así un kiosco manda lo de siempre. */
  private datosDeMedidas() {
    if (!this.tieneMedidas) return {};
    const presentaciones = (this.editor()?.lista() ?? []).map((p) => ({
      nombre: p.nombre,
      cantidadBase: p.cantidadBase,
      precio: p.precio,
    }));
    return { unidad: this.unidad(), presentaciones };
  }

  private aplicarValidadoresDeStock(): void {
    const regla: ValidatorFn[] = [Validators.required, Validators.min(0)];
    if (this.unidad() === 'Unidad') regla.push(Validators.pattern(/^\d+$/));
    else regla.push((c) => (c.value !== null && c.value !== '' && tieneMasDeTresDecimales(Number(c.value)) ? { decimales: true } : null));

    for (const control of [this.form.controls.stockActual, this.form.controls.stockMinimo]) {
      control.setValidators(regla);
      control.updateValueAndValidity({ emitEvent: false });
    }
  }
}
