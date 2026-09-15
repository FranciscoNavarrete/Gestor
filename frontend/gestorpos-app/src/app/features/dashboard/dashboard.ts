import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DashboardDto } from '../../core/models/reportes.models';
import { ReportesService } from '../../core/services/reportes.service';

@Component({
  selector: 'app-dashboard',
  imports: [DecimalPipe, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly reportesService = inject(ReportesService);

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
}
