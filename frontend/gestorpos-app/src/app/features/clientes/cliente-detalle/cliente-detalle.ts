import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import {
  ClienteDetalle as ClienteDetalleDto,
  ClienteVenta,
  MovimientoCuenta,
} from '../../../core/models/cliente.models';
import { ClienteService } from '../../../core/services/cliente.service';
import { extraerMensajeError } from '../../../core/utils/error.util';
import { RegistrarPagoDialog } from './registrar-pago-dialog/registrar-pago-dialog';

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
  private readonly dialog = inject(MatDialog);

  private readonly id = this.route.snapshot.paramMap.get('id')!;

  readonly cargando = signal(true);
  readonly cliente = signal<ClienteDetalleDto | null>(null);
  readonly nombreForm = signal('');
  readonly telefonoForm = signal('');
  readonly guardando = signal(false);

  readonly cargandoVentas = signal(true);
  readonly ventas = signal<ClienteVenta[]>([]);

  readonly cargandoMovimientos = signal(true);
  readonly movimientos = signal<MovimientoCuenta[]>([]);

  ngOnInit(): void {
    this.cargarCliente();
    this.cargarVentas();
    this.cargarMovimientosCuenta();
  }

  private cargarCliente(): void {
    this.cargando.set(true);
    this.clienteService.obtener(this.id).subscribe({
      next: (cliente) => {
        this.cliente.set(cliente);
        this.nombreForm.set(cliente.nombre ?? '');
        this.telefonoForm.set(cliente.telefono);
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

  private cargarMovimientosCuenta(): void {
    this.cargandoMovimientos.set(true);
    this.clienteService.listarMovimientosCuenta(this.id).subscribe({
      next: (movimientos) => {
        this.movimientos.set(movimientos);
        this.cargandoMovimientos.set(false);
      },
      error: () => this.cargandoMovimientos.set(false),
    });
  }

  abrirRegistrarPago(): void {
    const c = this.cliente();
    if (!c || c.saldoCuentaCorriente <= 0) return;

    this.dialog
      .open(RegistrarPagoDialog, {
        data: { clienteId: this.id, nombreCliente: c.nombre || c.telefono, saldo: c.saldoCuentaCorriente },
        width: '420px',
        maxWidth: '95vw',
      })
      .afterClosed()
      .subscribe((clienteActualizado: ClienteDetalleDto | undefined) => {
        if (!clienteActualizado) return;
        this.cliente.set(clienteActualizado);
        this.cargarMovimientosCuenta();
        this.snackBar.open('Pago registrado', 'Cerrar', { duration: 3000 });
      });
  }

  guardar(): void {
    if (this.guardando()) return;

    this.guardando.set(true);
    this.clienteService
      .editar(this.id, { nombre: this.nombreForm().trim() || null, telefono: this.telefonoForm().trim() })
      .subscribe({
        next: (cliente) => {
          this.guardando.set(false);
          this.cliente.update((actual) =>
            actual ? { ...actual, nombre: cliente.nombre, telefono: cliente.telefono } : actual,
          );
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
