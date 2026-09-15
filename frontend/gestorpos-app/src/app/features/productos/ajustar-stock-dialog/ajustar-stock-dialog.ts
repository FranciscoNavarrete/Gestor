import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Producto } from '../../../core/models/catalog.models';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

@Component({
  selector: 'app-ajustar-stock-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  templateUrl: './ajustar-stock-dialog.html',
  styleUrl: './ajustar-stock-dialog.scss',
})
export class AjustarStockDialog {
  private readonly fb = inject(FormBuilder);
  private readonly catalogoService = inject(CatalogoService);
  private readonly dialogRef = inject(MatDialogRef<AjustarStockDialog>);
  readonly producto = inject<Producto>(MAT_DIALOG_DATA);

  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    cantidad: [0, [Validators.required]],
  });

  guardar(): void {
    const cantidad = this.form.getRawValue().cantidad;
    if (this.form.invalid || this.guardando() || cantidad === 0) return;

    this.guardando.set(true);
    this.error.set(null);

    this.catalogoService.ajustarStock(this.producto.id, cantidad).subscribe({
      next: (producto) => this.dialogRef.close(producto),
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }
}
