import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import { Cliente } from '../../core/models/cliente.models';
import { ClienteService } from '../../core/services/cliente.service';

const TAMANO_PAGINA = 20;
const DEBOUNCE_BUSQUEDA_MS = 350;

@Component({
  selector: 'app-clientes',
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './clientes.html',
  styleUrl: './clientes.scss',
})
export class Clientes implements OnInit, OnDestroy {
  private readonly clienteService = inject(ClienteService);
  private readonly router = inject(Router);

  private debounceTimer?: ReturnType<typeof setTimeout>;

  readonly cargando = signal(true);
  readonly clientes = signal<Cliente[]>([]);
  readonly busqueda = signal('');

  readonly pagina = signal(1);
  readonly totalPaginas = signal(1);
  readonly totalItems = signal(0);

  ngOnInit(): void {
    this.cargar();
  }

  ngOnDestroy(): void {
    clearTimeout(this.debounceTimer);
  }

  cargar(): void {
    this.cargando.set(true);
    this.clienteService.buscar(this.busqueda(), this.pagina(), TAMANO_PAGINA).subscribe({
      next: (resultado) => {
        this.clientes.set(resultado.items);
        this.totalPaginas.set(resultado.totalPaginas);
        this.totalItems.set(resultado.totalItems);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }

  onBusquedaChange(valor: string): void {
    this.busqueda.set(valor);
    clearTimeout(this.debounceTimer);
    this.debounceTimer = setTimeout(() => {
      this.pagina.set(1);
      this.cargar();
    }, DEBOUNCE_BUSQUEDA_MS);
  }

  paginaAnterior(): void {
    if (this.pagina() <= 1) return;
    this.pagina.set(this.pagina() - 1);
    this.cargar();
  }

  paginaSiguiente(): void {
    if (this.pagina() >= this.totalPaginas()) return;
    this.pagina.set(this.pagina() + 1);
    this.cargar();
  }

  abrirDetalle(cliente: Cliente): void {
    this.router.navigate(['/clientes', cliente.id]);
  }
}
