import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerInputEvent, MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { AdminUsuario, CATALOGO_FEATURES, MovimientoAdmin, MovimientosAdmin } from '../../../core/models/admin.models';
import { AdminService } from '../../../core/services/admin.service';
import { extraerMensajeError } from '../../../core/utils/error.util';

type Preset = 'hoy' | 'semana' | 'mes' | 'mes-pasado';
type Tono = 'ok' | 'aviso' | 'peligro' | 'info';

const ahora = (): Date => {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), d.getDate());
};

const formatoApi = (d: Date): string =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

// Cómo se muestra cada acción: verbo, ícono y color. El detalle (si hay) va debajo.
const ACCIONES: Record<string, { verbo: string; icono: string; tono: Tono }> = {
  'negocio.alta': { verbo: 'dio de alta el negocio', icono: 'add_business', tono: 'ok' },
  'negocio.activado': { verbo: 'reactivó el negocio', icono: 'toggle_on', tono: 'ok' },
  'negocio.desactivado': { verbo: 'desactivó el negocio', icono: 'toggle_off', tono: 'aviso' },
  'feature.activada': { verbo: 'activó la función', icono: 'tune', tono: 'info' },
  'feature.desactivada': { verbo: 'desactivó la función', icono: 'tune', tono: 'aviso' },
  'usuario.creado': { verbo: 'creó al usuario', icono: 'person_add', tono: 'info' },
  'usuario.activado': { verbo: 'reactivó al usuario', icono: 'how_to_reg', tono: 'ok' },
  'usuario.desactivado': { verbo: 'desactivó al usuario', icono: 'person_off', tono: 'peligro' },
  'negocio.nueva_suscripcion': { verbo: 'creó una suscripción nueva para el negocio', icono: 'autorenew', tono: 'ok' },
  'negocio.cambio_plan': { verbo: 'cambió el plan del negocio', icono: 'swap_horiz', tono: 'info' },
  'pago.confirmado': { verbo: 'confirmó el pago de', icono: 'paid', tono: 'ok' },
  'efectivo.entrega': { verbo: 'registró una entrega de efectivo de', icono: 'payments', tono: 'ok' },
  'efectivo.entrega_anulada': { verbo: 'anuló una entrega de efectivo de', icono: 'undo', tono: 'aviso' },
  'liquidacion.cerrada': { verbo: 'liquidó las comisiones de', icono: 'request_quote', tono: 'info' },
  'liquidacion.pagada': { verbo: 'marcó como pagadas las comisiones de', icono: 'paid', tono: 'ok' },
  'liquidacion.anulada': { verbo: 'anuló la liquidación de', icono: 'undo', tono: 'aviso' },
};

@Component({
  selector: 'app-movimientos-admin',
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
  templateUrl: './movimientos.html',
  styleUrl: './movimientos.scss',
})
export class MovimientosAdminPantalla implements OnInit, OnDestroy {
  private readonly adminService = inject(AdminService);

  readonly hoy = ahora();
  readonly esMovil = window.innerWidth < 600;
  readonly desde = new FormControl<Date | null>(null);
  readonly hasta = new FormControl<Date | null>(null);
  readonly adminId = new FormControl<string | null>(null);
  readonly accion = new FormControl<string | null>(null);
  readonly texto = new FormControl('', { nonNullable: true });
  readonly presetActivo = signal<Preset | null>('mes');
  readonly usuarios = signal<AdminUsuario[]>([]);
  readonly resultado = signal<MovimientosAdmin | null>(null);
  readonly cargando = signal(false);
  readonly error = signal<string | null>(null);

  readonly presets: { clave: Preset; etiqueta: string }[] = [
    { clave: 'hoy', etiqueta: 'Hoy' },
    { clave: 'semana', etiqueta: '7 días' },
    { clave: 'mes', etiqueta: 'Este mes' },
    { clave: 'mes-pasado', etiqueta: 'Mes pasado' },
  ];

  readonly opcionesAccion: { valor: string | null; etiqueta: string }[] = [
    { valor: null, etiqueta: 'Todas las acciones' },
    { valor: 'negocio.alta', etiqueta: 'Altas de negocios' },
    { valor: 'negocio.activado', etiqueta: 'Negocios reactivados' },
    { valor: 'negocio.desactivado', etiqueta: 'Negocios desactivados' },
    { valor: 'negocio.nueva_suscripcion', etiqueta: 'Suscripciones nuevas' },
    { valor: 'negocio.cambio_plan', etiqueta: 'Cambios de plan' },
    { valor: 'feature', etiqueta: 'Funciones de negocios' },
    { valor: 'usuario', etiqueta: 'Usuarios del panel' },
    { valor: 'liquidacion', etiqueta: 'Liquidaciones' },
  ];

  private temporizadorBusqueda: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.adminService.listarUsuarios().subscribe({
      next: (usuarios) => this.usuarios.set(usuarios),
      error: () => {},
    });
    this.aplicarPreset('mes');
  }

  ngOnDestroy(): void {
    if (this.temporizadorBusqueda) clearTimeout(this.temporizadorBusqueda);
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

  // La búsqueda espera a que se deje de escribir un instante antes de consultar.
  alEscribirBusqueda(): void {
    if (this.temporizadorBusqueda) clearTimeout(this.temporizadorBusqueda);
    this.temporizadorBusqueda = setTimeout(() => this.cargar(), 400);
  }

  cargar(): void {
    const desde = this.desde.value;
    const hasta = this.hasta.value;
    if (!desde || !hasta) return;

    this.cargando.set(true);
    this.error.set(null);
    this.adminService
      .obtenerMovimientos(formatoApi(desde), formatoApi(hasta), this.adminId.value, this.accion.value, this.texto.value)
      .subscribe({
        next: (resultado) => {
          this.resultado.set(resultado);
          this.cargando.set(false);
        },
        error: (err) => {
          this.cargando.set(false);
          this.resultado.set(null);
          this.error.set(extraerMensajeError(err));
        },
      });
  }

  verbo(m: MovimientoAdmin): string {
    return ACCIONES[m.accion]?.verbo ?? m.accion;
  }

  icono(m: MovimientoAdmin): string {
    return ACCIONES[m.accion]?.icono ?? 'history';
  }

  tono(m: MovimientoAdmin): Tono {
    return ACCIONES[m.accion]?.tono ?? 'info';
  }

  // Una función de un negocio: el detalle trae la clave; se muestra con su nombre.
  nombreFuncion(m: MovimientoAdmin): string | null {
    if (!m.accion.startsWith('feature.') || !m.detalle) return null;
    return CATALOGO_FEATURES.find((f) => f.clave === m.detalle)?.nombre ?? m.detalle;
  }

  // El detalle de un usuario es su rol; el de un negocio, cómo se cobra.
  detalle(m: MovimientoAdmin): string | null {
    if (m.accion.startsWith('feature.')) return null;
    return m.detalle;
  }

  // "hoy", "ayer" o dd/MM, siempre en hora de Argentina.
  dia(fechaUtc: string): string {
    const local = new Date(new Date(fechaUtc).getTime() - 3 * 3600 * 1000);
    const ahoraAr = new Date(Date.now() - 3 * 3600 * 1000);
    const clave = (d: Date) => d.toISOString().slice(0, 10);
    if (clave(local) === clave(ahoraAr)) return 'hoy';
    const ayer = new Date(ahoraAr.getTime() - 24 * 3600 * 1000);
    if (clave(local) === clave(ayer)) return 'ayer';
    return '';
  }
}
