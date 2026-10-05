import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerInputEvent, MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { AdminUsuario, EstadoVenta, ReporteVentas } from '../../../core/models/admin.models';
import { AdminAuthService } from '../../../core/services/admin-auth.service';
import { AdminService } from '../../../core/services/admin.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

type Preset = 'hoy' | 'semana' | 'mes' | 'mes-pasado';

const ETIQUETAS_ESTADO: Record<EstadoVenta, string> = {
  suscripto: 'Suscripto',
  baja: 'Baja',
  esperando: 'Esperando cobro',
  pendiente: 'Sin autorizar',
  cancelada: 'Sin pagar',
};

const ahora = (): Date => {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), d.getDate());
};

const formatoApi = (d: Date): string =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

@Component({
  selector: 'app-reportes-admin',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './reportes.html',
  styleUrl: './reportes.scss',
})
export class ReportesAdmin implements OnInit {
  private readonly adminService = inject(AdminService);
  protected readonly adminAuth = inject(AdminAuthService);

  readonly hoy = ahora();
  readonly esMovil = window.innerWidth < 600;

  readonly desde = new FormControl<Date | null>(null);
  readonly hasta = new FormControl<Date | null>(null);
  readonly vendedorId = new FormControl<string | null>(null);

  readonly presetActivo = signal<Preset | null>('mes');
  readonly vendedores = signal<AdminUsuario[]>([]);
  readonly reporte = signal<ReporteVentas | null>(null);
  readonly cargando = signal(false);
  readonly error = signal<string | null>(null);

  readonly presets: { clave: Preset; etiqueta: string }[] = [
    { clave: 'hoy', etiqueta: 'Hoy' },
    { clave: 'semana', etiqueta: '7 días' },
    { clave: 'mes', etiqueta: 'Este mes' },
    { clave: 'mes-pasado', etiqueta: 'Mes pasado' },
  ];

  readonly maximoVentasVendedores = computed(() =>
    Math.max(1, ...(this.reporte()?.vendedores ?? []).map((v) => v.ventas)),
  );

  readonly detalleComision = computed(() => {
    const r = this.reporte()?.resumen;
    if (!r || r.ventas === 0) return '';
    const partes: string[] = [];
    if (r.ventasPrimeras > 0) partes.push(`${r.ventasPrimeras} × ${this.dinero(r.tarifaPrimeras)}`);
    if (r.ventasSiguientes > 0) partes.push(`${r.ventasSiguientes} × ${this.dinero(r.tarifaSiguientes)}`);
    return partes.join(' + ');
  });

  ngOnInit(): void {
    if (this.adminAuth.esOperador()) this.cargarVendedores();
    this.aplicarPreset('mes');
  }

  dinero(valor: number): string {
    return '$' + Math.round(valor).toLocaleString('es-AR');
  }

  etiquetaEstado(estado: EstadoVenta): string {
    return ETIQUETAS_ESTADO[estado];
  }

  aplicarPreset(preset: Preset): void {
    const hoy = this.hoy;
    let desde = hoy;
    let hasta = hoy;
    if (preset === 'semana') {
      desde = new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate() - 6);
    } else if (preset === 'mes') {
      desde = new Date(hoy.getFullYear(), hoy.getMonth(), 1);
    } else if (preset === 'mes-pasado') {
      desde = new Date(hoy.getFullYear(), hoy.getMonth() - 1, 1);
      hasta = new Date(hoy.getFullYear(), hoy.getMonth(), 0);
    }
    this.desde.setValue(desde);
    this.hasta.setValue(hasta);
    this.presetActivo.set(preset);
    this.cargar();
  }

  // Si se escribe una fecha que no corresponde, se corrige sola a la más cercana válida (el
  // calendario ya no deja elegirla): "hasta" nunca pasa de hoy y "desde" nunca pasa de "hasta".
  alCambiarDesde(evento: MatDatepickerInputEvent<Date>): void {
    let valor = evento.value;
    if (!valor) return;
    const hasta = this.hasta.value ?? this.hoy;
    if (valor > hasta) valor = hasta;
    if (valor > this.hoy) valor = this.hoy;
    this.desde.setValue(valor);
    this.presetActivo.set(null);
    this.cargar();
  }

  alCambiarHasta(evento: MatDatepickerInputEvent<Date>): void {
    let valor = evento.value;
    if (!valor) return;
    if (valor > this.hoy) valor = this.hoy;
    const desde = this.desde.value;
    if (desde && valor < desde) valor = desde;
    this.hasta.setValue(valor);
    this.presetActivo.set(null);
    this.cargar();
  }

  alCambiarVendedor(): void {
    this.cargar();
  }

  cargar(): void {
    const desde = this.desde.value;
    const hasta = this.hasta.value;
    if (!desde || !hasta) return;

    this.cargando.set(true);
    this.error.set(null);
    this.adminService.obtenerReporteVentas(formatoApi(desde), formatoApi(hasta), this.vendedorId.value).subscribe({
      next: (reporte) => {
        this.reporte.set(reporte);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        this.reporte.set(null);
        this.error.set(extraerMensajeError(err));
      },
    });
  }

  private cargarVendedores(): void {
    this.adminService.listarUsuarios().subscribe({
      next: (usuarios) => this.vendedores.set(usuarios.filter((u) => u.rol === 'Vendedor')),
      error: () => {},
    });
  }
}
