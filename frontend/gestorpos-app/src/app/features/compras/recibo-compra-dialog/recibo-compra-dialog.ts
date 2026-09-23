import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { CompraDto } from '../../../core/models/compra.models';

@Component({
  selector: 'app-recibo-compra-dialog',
  imports: [DatePipe, DecimalPipe, MatButtonModule, MatDialogModule, MatIconModule],
  templateUrl: './recibo-compra-dialog.html',
  styleUrl: './recibo-compra-dialog.scss',
})
export class ReciboCompraDialog {
  readonly compra = inject<CompraDto>(MAT_DIALOG_DATA);
}
