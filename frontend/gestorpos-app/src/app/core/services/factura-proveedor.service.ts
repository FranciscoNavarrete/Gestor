import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CrearFacturaProveedorRequest,
  FacturaProveedorDto,
  RegistrarPagoFacturaRequest,
  ResumenFacturasProveedorDto,
} from '../models/factura-proveedor.models';

@Injectable({ providedIn: 'root' })
export class FacturaProveedorService {
  constructor(private readonly http: HttpClient) {}

  crear(request: CrearFacturaProveedorRequest): Observable<FacturaProveedorDto> {
    return this.http.post<FacturaProveedorDto>(`${environment.apiUrl}/facturas-proveedor`, request);
  }

  listar(
    proveedorId?: string,
    desde?: string,
    hasta?: string,
    incluirPagadas = false,
  ): Observable<FacturaProveedorDto[]> {
    return this.http.get<FacturaProveedorDto[]>(`${environment.apiUrl}/facturas-proveedor`, {
      params: {
        ...(proveedorId && { proveedorId }),
        ...(desde && { desde }),
        ...(hasta && { hasta }),
        incluirPagadas,
      },
    });
  }

  resumen(): Observable<ResumenFacturasProveedorDto> {
    return this.http.get<ResumenFacturasProveedorDto>(`${environment.apiUrl}/facturas-proveedor/resumen`);
  }

  registrarPago(id: string, request: RegistrarPagoFacturaRequest): Observable<FacturaProveedorDto> {
    return this.http.post<FacturaProveedorDto>(`${environment.apiUrl}/facturas-proveedor/${id}/pagos`, request);
  }

  exportarPdf(proveedorId?: string, desde?: string, hasta?: string, incluirPagadas = false): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/reportes/facturas-proveedor/pdf`, {
      params: {
        ...(proveedorId && { proveedorId }),
        ...(desde && { desde }),
        ...(hasta && { hasta }),
        incluirPagadas,
      },
      responseType: 'blob',
    });
  }
}
