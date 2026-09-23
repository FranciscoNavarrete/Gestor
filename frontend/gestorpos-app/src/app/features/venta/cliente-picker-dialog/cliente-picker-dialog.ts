import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Cliente } from '../../../core/models/cliente.models';
import { ClienteService } from '../../../core/services/cliente.service';

const TAMANO_SUGERENCIAS = 5;
const DEBOUNCE_MS = 300;

export interface ClientePickerDialogData {
  nombre: string;
  telefono: string;
}

export interface ClientePickerResultado {
  nombre: string;
  telefono: string;
  cliente: Cliente | null;
}

@Component({
  selector: 'app-cliente-picker-dialog',
  imports: [
    FormsModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './cliente-picker-dialog.html',
  styleUrl: './cliente-picker-dialog.scss',
})
export class ClientePickerDialog implements OnDestroy {
  private readonly clienteService = inject(ClienteService);
  private readonly dialogRef = inject(MatDialogRef<ClientePickerDialog>);
  readonly data = inject<ClientePickerDialogData>(MAT_DIALOG_DATA);

  readonly teniaCliente = !!this.data.telefono.trim();

  readonly nombre = signal(this.data.nombre);
  readonly telefono = signal(this.data.telefono);
  readonly sugerencias = signal<Cliente[]>([]);
  readonly clienteSeleccionado = signal<Cliente | null>(null);

  private debounceTimer?: ReturnType<typeof setTimeout>;

  ngOnDestroy(): void {
    clearTimeout(this.debounceTimer);
  }

  // El trigger de mat-autocomplete escribe el valor crudo de la opción seleccionada (un Cliente)
  // en el ngModel antes de que dispare (optionSelected) — acá lo ignoramos y dejamos que
  // seleccionar() (más abajo) sea quien fije el string correcto.
  onNombreChange(valor: string | Cliente): void {
    if (typeof valor !== 'string') return;
    this.nombre.set(valor);
    this.clienteSeleccionado.set(null);
    this.buscarDebounced(valor);
  }

  onTelefonoChange(valor: string | Cliente): void {
    if (typeof valor !== 'string') return;
    this.telefono.set(valor);
    this.clienteSeleccionado.set(null);
    this.buscarDebounced(valor);
  }

  mostrarNombreCliente = (valor: string | Cliente): string =>
    typeof valor === 'string' ? valor : (valor?.nombre ?? '');

  mostrarTelefonoCliente = (valor: string | Cliente): string =>
    typeof valor === 'string' ? valor : (valor?.telefono ?? '');

  private buscarDebounced(valor: string): void {
    clearTimeout(this.debounceTimer);

    const termino = valor.trim();
    if (termino.length < 2) {
      this.sugerencias.set([]);
      return;
    }

    this.debounceTimer = setTimeout(() => {
      this.clienteService.buscar(termino, 1, TAMANO_SUGERENCIAS).subscribe({
        next: (resultado) => this.sugerencias.set(resultado.items),
        error: () => this.sugerencias.set([]),
      });
    }, DEBOUNCE_MS);
  }

  seleccionar(cliente: Cliente): void {
    this.nombre.set(cliente.nombre ?? '');
    this.telefono.set(cliente.telefono);
    this.clienteSeleccionado.set(cliente);
    this.sugerencias.set([]);
  }

  guardar(): void {
    const telefono = this.telefono().trim();
    if (!telefono) return;

    this.dialogRef.close({
      nombre: this.nombre().trim(),
      telefono,
      cliente: this.clienteSeleccionado(),
    } satisfies ClientePickerResultado);
  }

  quitar(): void {
    this.dialogRef.close(null);
  }
}
