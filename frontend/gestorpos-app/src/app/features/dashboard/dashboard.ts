import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DashboardDto } from '../../core/models/reportes.models';
import { ReportesService } from '../../core/services/reportes.service';
import { fechaAIso, hoy } from '../../core/utils/fecha.util';

@Component({
  selector: 'app-dashboard',
  imports: [DecimalPipe, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly reportesService = inject(ReportesService);
  private readonly router = inject(Router);

  readonly cargando = signal(true);
  readonly datos = signal<DashboardDto | null>(null);

  ngOnInit(): void {
    this.reportesService.dashboard().subscribe({
      next: (datos) => {
        this.datos.set(datos);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false),
    });
  }

  irAVentasDeHoy(): void {
    const iso = fechaAIso(hoy());
    this.router.navigate(['/reportes'], { queryParams: { vista: 'ventas', desde: iso, hasta: iso } });
  }

  irAVentasDelMes(): void {
    const hoyFecha = hoy();
    const primerDiaMes = new Date(hoyFecha.getFullYear(), hoyFecha.getMonth(), 1);
    this.router.navigate(['/reportes'], {
      queryParams: { vista: 'ventas', desde: fechaAIso(primerDiaMes), hasta: fechaAIso(hoyFecha) },
    });
  }

  irAStockBajo(): void {
    this.router.navigate(['/productos'], { queryParams: { soloBajoStock: 'true' } });
  }
}
