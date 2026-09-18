import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PaginaMovimientosStock } from '../models/movimiento-stock.models';

@Injectable({ providedIn: 'root' })
export class MovimientoStockService {
  constructor(private readonly http: HttpClient) {}

  buscar(
    productoId: string | undefined,
    desde: string | undefined,
    hasta: string | undefined,
    pagina: number,
    tamanoPagina: number,
  ): Observable<PaginaMovimientosStock> {
    const params: Record<string, string | number> = { pagina, tamanoPagina };
    if (productoId) params['productoId'] = productoId;
    if (desde) params['desde'] = desde;
    if (hasta) params['hasta'] = hasta;

    return this.http.get<PaginaMovimientosStock>(`${environment.apiUrl}/movimientos-stock`, { params });
  }
}
