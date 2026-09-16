import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { ClienteDetalle as ClienteDetalleDto, ClienteVenta } from '../../../core/models/cliente.models';
import { ClienteService } from '../../../core/services/cliente.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

@Component({
  selector: 'app-cliente-detalle',
  imports: [
    DatePipe,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './cliente-detalle.html',
  styleUrl: './cliente-detalle.scss',
})
export class ClienteDetalle implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly clienteService = inject(ClienteService);
  private readonly snackBar = inject(MatSnackBar);

  private readonly id = this.route.snapshot.paramMap.get('id')!;

  readonly cargando = signal(true);
  readonly cliente = signal<ClienteDetalleDto | null>(null);
  readonly nombreForm = signal('');
  readonly guardando = signal(false);

  readonly cargandoVentas = signal(true);
  readonly ventas = signal<ClienteVenta[]>([]);

  ngOnInit(): void {
    this.cargarCliente();
    this.cargarVentas();
  }

  private cargarCliente(): void {
    this.cargando.set(true);
    this.clienteService.obtener(this.id).subscribe({
      next: (cliente) => {
        this.cliente.set(cliente);
        this.nombreForm.set(cliente.nombre ?? '');
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }

  private cargarVentas(): void {
    this.cargandoVentas.set(true);
    this.clienteService.listarVentas(this.id).subscribe({
      next: (ventas) => {
        this.ventas.set(ventas);
        this.cargandoVentas.set(false);
      },
      error: () => this.cargandoVentas.set(false),
    });
  }

  guardar(): void {
    if (this.guardando()) return;

    this.guardando.set(true);
    this.clienteService.editar(this.id, { nombre: this.nombreForm().trim() || null }).subscribe({
      next: (cliente) => {
        this.guardando.set(false);
        this.cliente.update((actual) => (actual ? { ...actual, nombre: cliente.nombre } : actual));
        this.snackBar.open('Cliente guardado', 'Cerrar', { duration: 3000 });
      },
      error: (err) => {
        this.guardando.set(false);
        this.snackBar.open(extraerMensajeError(err), 'Cerrar', { duration: 4000 });
      },
    });
  }

  volver(): void {
    this.router.navigate(['/clientes']);
  }
}
