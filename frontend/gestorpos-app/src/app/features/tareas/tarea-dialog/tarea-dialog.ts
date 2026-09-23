import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TareaDto } from '../../../core/models/tarea.models';
import { TareaService } from '../../../core/services/tarea.service';
import { extraerMensajeError } from '../../../core/utils/error.util';
import { fechaAIso, hoy } from '../../../core/utils/fecha.util';

export interface TareaDialogData {
  tarea: TareaDto | null;
}

function horaDesdeFecha(fecha: Date): string {
  const h = String(fecha.getHours()).padStart(2, '0');
  const m = String(fecha.getMinutes()).padStart(2, '0');
  return `${h}:${m}`;
}

const HORA_REGEX = /^\d{2}:\d{2}$/;

@Component({
  selector: 'app-tarea-dialog',
  imports: [
    FormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './tarea-dialog.html',
  styleUrl: './tarea-dialog.scss',
})
export class TareaDialog {
  private readonly tareaService = inject(TareaService);
  private readonly dialogRef = inject(MatDialogRef<TareaDialog>);
  readonly data = inject<TareaDialogData>(MAT_DIALOG_DATA);

  readonly esEdicion = !!this.data.tarea;
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  readonly titulo = signal(this.data.tarea?.titulo ?? '');
  readonly notas = signal(this.data.tarea?.notas ?? '');
  readonly fecha = signal<Date>(this.data.tarea ? new Date(this.data.tarea.fechaHora) : hoy());
  readonly hora = signal(this.data.tarea ? horaDesdeFecha(new Date(this.data.tarea.fechaHora)) : '09:00');
  readonly minutosAntesAviso = signal<number | null>(this.data.tarea?.minutosAntesAviso ?? null);

  readonly esValido = computed(() => !!this.titulo().trim() && !!this.fecha() && HORA_REGEX.test(this.hora()));

  readonly resumenAviso = computed(() => {
    const minutos = this.minutosAntesAviso();
    const fecha = this.fecha();
    const hora = this.hora();
    if (minutos === null || !fecha || !HORA_REGEX.test(hora)) return null;

    const [h, m] = hora.split(':').map(Number);
    const momentoTarea = new Date(fecha);
    momentoTarea.setHours(h, m, 0, 0);
    const momentoAviso = new Date(momentoTarea.getTime() - minutos * 60000);

    const fechaTexto = momentoAviso.toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' });
    const horaTexto = `${String(momentoAviso.getHours()).padStart(2, '0')}:${String(momentoAviso.getMinutes()).padStart(2, '0')}`;
    return `${fechaTexto} a las ${horaTexto}`;
  });

  guardar(): void {
    if (!this.esValido() || this.guardando()) return;

    this.guardando.set(true);
    this.error.set(null);

    const request = {
      titulo: this.titulo().trim(),
      notas: this.notas().trim() || null,
      fecha: fechaAIso(this.fecha()),
      hora: this.hora(),
      minutosAntesAviso: this.minutosAntesAviso(),
    };

    const request$ = this.esEdicion
      ? this.tareaService.editar(this.data.tarea!.id, request)
      : this.tareaService.crear(request);

    request$.subscribe({
      next: (tarea: TareaDto) => this.dialogRef.close(tarea),
      error: (err) => {
        this.guardando.set(false);
        this.error.set(extraerMensajeError(err));
      },
    });
  }
}
