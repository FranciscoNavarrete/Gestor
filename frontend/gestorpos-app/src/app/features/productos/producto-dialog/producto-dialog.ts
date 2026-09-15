import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Categoria, Producto } from '../../../core/models/catalog.models';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { extraerMensajeError } from '../../../core/utils/error.util';
import { CategoriaDialog } from '../categoria-dialog/categoria-dialog';

export interface ProductoDialogData {
  producto: Producto | null;
  categorias: Categoria[];
}

@Component({
  selector: 'app-producto-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './producto-dialog.html',
  styleUrl: './producto-dialog.scss',
})
export class ProductoDialog {
  private readonly fb = inject(FormBuilder);
  private readonly catalogoService = inject(CatalogoService);
  private readonly dialog = inject(MatDialog);
  private readonly dialogRef = inject(MatDialogRef<ProductoDialog>);
  readonly data = inject<ProductoDialogData>(MAT_DIALOG_DATA);

  readonly esEdicion = this.data.producto !== null;
  readonly categorias = signal(this.data.categorias);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    sku: [this.data.producto?.sku ?? '', [Validators.required]],
    nombre: [this.data.producto?.nombre ?? '', [Validators.required]],
    categoriaId: this.fb.control<string | null>(this.data.producto?.categoriaId ?? null),
    precio: [this.data.producto?.precio ?? 0, [Validators.required, Validators.min(0)]],
    costo: [this.data.producto?.costo ?? 0, [Validators.required, Validators.min(0)]],
    stockActual: [this.data.producto?.stockActual ?? 0, [Validators.required, Validators.min(0)]],
    stockMinimo: [this.data.producto?.stockMinimo ?? 0, [Validators.required, Validators.min(0)]],
  });

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

  guardar(): void {
    if (this.form.invalid || this.guardando()) return;

    this.guardando.set(true);
    this.error.set(null);
    const valores = this.form.getRawValue();

    const request$ = this.esEdicion
      ? this.catalogoService.editarProducto(this.data.producto!.id, {
          nombre: valores.nombre,
          categoriaId: valores.categoriaId,
          precio: valores.precio,
          costo: valores.costo,
          stockMinimo: valores.stockMinimo,
        })
      : this.catalogoService.crearProducto(valores);

    request$.subscribe({
      next: (producto) => this.dialogRef.close(producto),
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }
}
