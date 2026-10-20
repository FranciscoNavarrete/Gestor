import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { catchError, of, startWith, switchMap } from 'rxjs';
import { AlcancePreciosMasivo, Categoria, Marca } from '../../../core/models/catalog.models';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { formatearNumero } from '../../../core/utils/cantidad.util';
import { extraerMensajeError } from '../../../core/utils/error.util';

export interface ActualizarPreciosDialogData {
  categorias: Categoria[];
  marcas: Marca[];
}

@Component({
  selector: 'app-actualizar-precios-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './actualizar-precios-dialog.html',
  styleUrl: './actualizar-precios-dialog.scss',
})
export class ActualizarPreciosDialog {
  private readonly fb = inject(FormBuilder);
  private readonly catalogoService = inject(CatalogoService);
  private readonly dialogRef = inject(MatDialogRef<ActualizarPreciosDialog>);
  protected readonly data = inject<ActualizarPreciosDialogData>(MAT_DIALOG_DATA);

  readonly categorias = this.data.categorias.filter((c) => c.activo);
  readonly marcas = this.data.marcas.filter((m) => m.activo);

  readonly alcance = signal<AlcancePreciosMasivo | null>(null);
  readonly aplicando = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.group({
    porcentaje: this.fb.control<number | null>(null, [Validators.required, Validators.min(-90), Validators.max(1000)]),
    categoriaId: this.fb.control<string | null>(null),
    marcaId: this.fb.control<string | null>(null),
  });

  constructor() {
    // Cada vez que se cambia la categoría o la marca se vuelve a contar a cuántos productos alcanza.
    this.form.valueChanges
      .pipe(
        startWith(null),
        switchMap(() => {
          this.alcance.set(null);
          const { categoriaId, marcaId } = this.form.getRawValue();
          return this.catalogoService.alcancePreciosMasivo(categoriaId, marcaId).pipe(catchError(() => of(null)));
        }),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe((alcance) => this.alcance.set(alcance));
  }

  /** «+10%» o «-5%», para el botón y el ejemplo. */
  get textoPorcentaje(): string {
    const p = this.form.controls.porcentaje.value;
    if (p === null || !Number.isFinite(p)) return '';
    return `${p > 0 ? '+' : ''}${formatearNumero(p)}%`;
  }

  /** Cómo quedaría un producto de $1.000, para que se vea el efecto antes de aplicar. */
  get ejemplo(): string | null {
    const p = this.form.controls.porcentaje.value;
    if (p === null || !Number.isFinite(p) || this.form.controls.porcentaje.invalid || p === 0) return null;
    const nuevo = Math.round(1000 * (1 + p / 100) * 100) / 100;
    return `Un producto de $1.000 pasaría a $${nuevo.toLocaleString('es-AR')}.`;
  }

  get textoAlcance(): string {
    const a = this.alcance();
    if (!a) return 'Contando…';
    if (a.productos === 0) return 'No hay productos activos con ese filtro.';
    const productos = `${a.productos} producto${a.productos === 1 ? '' : 's'}`;
    const presentaciones = a.presentaciones > 0 ? ` y ${a.presentaciones} presentacion${a.presentaciones === 1 ? '' : 'es'}` : '';
    return `Alcanza a ${productos}${presentaciones}.`;
  }

  get puedeAplicar(): boolean {
    const p = this.form.controls.porcentaje.value;
    return this.form.valid && p !== 0 && (this.alcance()?.productos ?? 0) > 0 && !this.aplicando();
  }

  aplicar(): void {
    if (!this.puedeAplicar) return;
    this.aplicando.set(true);
    this.error.set(null);
    const { porcentaje, categoriaId, marcaId } = this.form.getRawValue();

    this.catalogoService.actualizarPreciosMasivo({ porcentaje: porcentaje!, categoriaId, marcaId }).subscribe({
      next: (resultado) => this.dialogRef.close(resultado),
      error: (err) => {
        this.aplicando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }
}
