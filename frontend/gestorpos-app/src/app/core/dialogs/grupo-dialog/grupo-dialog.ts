import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { GrupoDueno, TenantResumen } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';
import { extraerMensajeError } from '../../utils/error.util';

export interface GrupoDialogData {
  negocio: TenantResumen;
}

/** Agrega un negocio a un grupo de dueños (existente o nuevo) o lo saca de su grupo. Cierra con el negocio actualizado. */
@Component({
  selector: 'app-grupo-dialog',
  imports: [FormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule],
  templateUrl: './grupo-dialog.html',
  styleUrl: './grupo-dialog.scss',
})
export class GrupoDialog implements OnInit {
  readonly data = inject<GrupoDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<GrupoDialog, TenantResumen>);
  private readonly adminService = inject(AdminService);

  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly grupos = signal<GrupoDueno[]>([]);
  readonly grupoId = signal<string | null>(this.data.negocio.grupoDuenoId ?? null);
  readonly creandoNuevo = signal(false);
  nombreNuevo = '';

  ngOnInit(): void {
    this.adminService.listarGrupos().subscribe({
      next: (grupos) => {
        this.grupos.set(grupos);
        // Si todavía no hay ningún grupo, lo único que se puede hacer es crear uno.
        if (grupos.length === 0) this.creandoNuevo.set(true);
        this.cargando.set(false);
      },
      error: (err) => {
        this.error.set(extraerMensajeError(err));
        this.cargando.set(false);
      },
    });
  }

  elegir(id: string | null): void {
    this.grupoId.set(id);
    this.creandoNuevo.set(false);
  }

  nuevo(): void {
    this.creandoNuevo.set(true);
    this.grupoId.set(null);
  }

  get puedeGuardar(): boolean {
    if (this.guardando()) return false;
    return this.creandoNuevo() ? this.nombreNuevo.trim().length > 0 : this.grupoId() !== (this.data.negocio.grupoDuenoId ?? null);
  }

  guardar(): void {
    if (!this.puedeGuardar) return;
    this.guardando.set(true);
    this.error.set(null);

    const asignar = (grupoId: string | null) =>
      this.adminService.asignarGrupo(this.data.negocio.id, grupoId).subscribe({
        next: (negocio) => this.dialogRef.close(negocio),
        error: (err) => this.fallar(err),
      });

    if (this.creandoNuevo()) {
      this.adminService.crearGrupo({ nombre: this.nombreNuevo.trim(), telefono: this.data.negocio.telefono ?? null }).subscribe({
        next: (grupo) => asignar(grupo.id),
        error: (err) => this.fallar(err),
      });
    } else {
      asignar(this.grupoId());
    }
  }

  private fallar(err: unknown): void {
    this.error.set(extraerMensajeError(err));
    this.guardando.set(false);
  }
}
