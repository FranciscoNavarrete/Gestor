import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Marca } from '../../../core/models/catalog.models';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

export interface MarcaDialogData {
  marca: Marca | null;
}

@Component({
  selector: 'app-marca-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  templateUrl: './marca-dialog.html',
  styleUrl: './marca-dialog.scss',
})
export class MarcaDialog {
  private readonly fb = inject(FormBuilder);
  private readonly catalogoService = inject(CatalogoService);
  private readonly dialogRef = inject(MatDialogRef<MarcaDialog>);
  private readonly data = inject<MarcaDialogData | null>(MAT_DIALOG_DATA, { optional: true });

  readonly esEdicion = !!this.data?.marca;
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    nombre: [this.data?.marca?.nombre ?? '', [Validators.required]],
  });

  guardar(): void {
    if (this.form.invalid || this.guardando()) return;

    this.guardando.set(true);
    this.error.set(null);

    const valores = this.form.getRawValue();
    const request$ = this.esEdicion
      ? this.catalogoService.editarMarca(this.data!.marca!.id, valores)
      : this.catalogoService.crearMarca(valores);

    request$.subscribe({
      next: (marca: Marca) => this.dialogRef.close(marca),
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }
}
