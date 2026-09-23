import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TareaDto } from '../../core/models/tarea.models';
import { TareaService } from '../../core/services/tarea.service';
import { extraerMensajeError } from '../../core/utils/error.util';
import { TareaDialog, TareaDialogData } from './tarea-dialog/tarea-dialog';

interface GrupoTareas {
  etiqueta: string;
  tareas: TareaDto[];
}

function esMismoDia(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

function etiquetaDia(fecha: Date): string {
  const hoy = new Date();
  const manana = new Date(hoy);
  manana.setDate(manana.getDate() + 1);

  if (esMismoDia(fecha, hoy)) return 'Hoy';
  if (esMismoDia(fecha, manana)) return 'Mañana';
  return fecha.toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' });
}

@Component({
  selector: 'app-tareas',
  imports: [DatePipe, MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './tareas.html',
  styleUrl: './tareas.scss',
})
export class Tareas implements OnInit {
  private readonly tareaService = inject(TareaService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly cargando = signal(true);
  readonly tareas = signal<TareaDto[]>([]);

  readonly tareasPendientes = computed(() => this.tareas().filter((t) => !t.completada));
  readonly tareasCompletadas = computed(() =>
    this.tareas()
      .filter((t) => t.completada)
      .sort((a, b) => new Date(b.fechaHora).getTime() - new Date(a.fechaHora).getTime()),
  );

  readonly tareasVencidas = computed(() => {
    const ahora = Date.now();
    return this.tareasPendientes()
      .filter((t) => new Date(t.fechaHora).getTime() < ahora)
      .sort((a, b) => new Date(a.fechaHora).getTime() - new Date(b.fechaHora).getTime());
  });

  readonly gruposProximas = computed((): GrupoTareas[] => {
    const ahora = Date.now();
    const proximas = this.tareasPendientes()
      .filter((t) => new Date(t.fechaHora).getTime() >= ahora)
      .sort((a, b) => new Date(a.fechaHora).getTime() - new Date(b.fechaHora).getTime());

    const grupos: GrupoTareas[] = [];
    for (const tarea of proximas) {
      const etiqueta = etiquetaDia(new Date(tarea.fechaHora));
      const grupoExistente = grupos.find((g) => g.etiqueta === etiqueta);
      if (grupoExistente) grupoExistente.tareas.push(tarea);
      else grupos.push({ etiqueta, tareas: [tarea] });
    }
    return grupos;
  });

  readonly vacio = computed(
    () => this.tareasVencidas().length === 0 && this.gruposProximas().length === 0 && this.tareasCompletadas().length === 0,
  );

  ngOnInit(): void {
    this.cargarTareas();
  }

  private cargarTareas(): void {
    this.cargando.set(true);
    this.tareaService.listar(true).subscribe({
      next: (tareas) => {
        this.tareas.set(tareas);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }

  nuevaTarea(): void {
    this.abrirDialogo(null);
  }

  editarTarea(tarea: TareaDto): void {
    this.abrirDialogo(tarea);
  }

  private abrirDialogo(tarea: TareaDto | null): void {
    const data: TareaDialogData = { tarea };
    this.dialog
      .open(TareaDialog, { data, width: '440px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((resultado) => {
        if (resultado) this.cargarTareas();
      });
  }

  toggleCompletada(tarea: TareaDto): void {
    const accion$ = tarea.completada ? this.tareaService.marcarPendiente(tarea.id) : this.tareaService.completar(tarea.id);
    accion$.subscribe({
      next: () => this.cargarTareas(),
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 }),
    });
  }

  eliminarTarea(tarea: TareaDto): void {
    if (!confirm(`¿Eliminar la tarea "${tarea.titulo}"?`)) return;

    this.tareaService.eliminar(tarea.id).subscribe({
      next: () => this.cargarTareas(),
      error: (err) => this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 }),
    });
  }
}
