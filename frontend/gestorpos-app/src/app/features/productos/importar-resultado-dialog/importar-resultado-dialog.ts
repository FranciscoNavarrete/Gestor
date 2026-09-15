import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { ImportarProductosResultado } from '../../../core/models/catalog.models';

@Component({
  selector: 'app-importar-resultado-dialog',
  imports: [MatButtonModule, MatDialogModule, MatIconModule],
  templateUrl: './importar-resultado-dialog.html',
  styleUrl: './importar-resultado-dialog.scss',
})
export class ImportarResultadoDialog {
  readonly resultado = inject<ImportarProductosResultado>(MAT_DIALOG_DATA);
}
