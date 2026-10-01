import { Component, OnInit, inject, signal } from '@angular/core';
import { toDataURL } from 'qrcode';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

export interface QrDialogData {
  nombre: string;
  link: string;
}

@Component({
  selector: 'app-qr-dialog',
  imports: [MatButtonModule, MatDialogModule, MatProgressSpinnerModule],
  templateUrl: './qr-dialog.html',
  styleUrl: './qr-dialog.scss',
})
export class QrDialog implements OnInit {
  readonly data = inject<QrDialogData>(MAT_DIALOG_DATA);

  readonly qrDataUrl = signal<string | null>(null);

  ngOnInit(): void {
    toDataURL(this.data.link, { width: 220, margin: 1 })
      .then((dataUrl) => this.qrDataUrl.set(dataUrl))
      .catch(() => {});
  }
}
