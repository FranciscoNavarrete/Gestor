import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CompraDto,
  CompraResumenDto,
  CrearCompraRequest,
  CrearProveedorRequest,
  EditarProveedorRequest,
  Proveedor,
  ResumenComprasDto,
} from '../models/compra.models';

@Injectable({ providedIn: 'root' })
export class CompraService {
  constructor(private readonly http: HttpClient) {}

  listarProveedores(): Observable<Proveedor[]> {
    return this.http.get<Proveedor[]>(`${environment.apiUrl}/proveedores`);
  }

  crearProveedor(request: CrearProveedorRequest): Observable<Proveedor> {
    return this.http.post<Proveedor>(`${environment.apiUrl}/proveedores`, request);
  }

  editarProveedor(id: string, request: EditarProveedorRequest): Observable<Proveedor> {
    return this.http.put<Proveedor>(`${environment.apiUrl}/proveedores/${id}`, request);
  }

  desactivarProveedor(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/proveedores/${id}`);
  }

  activarProveedor(id: string): Observable<Proveedor> {
    return this.http.post<Proveedor>(`${environment.apiUrl}/proveedores/${id}/activar`, {});
  }

  crearCompra(request: CrearCompraRequest): Observable<CompraDto> {
    return this.http.post<CompraDto>(`${environment.apiUrl}/compras`, request);
  }

  obtenerCompra(id: string): Observable<CompraDto> {
    return this.http.get<CompraDto>(`${environment.apiUrl}/compras/${id}`);
  }

  listarCompras(desde?: string, hasta?: string): Observable<CompraResumenDto[]> {
    return this.http.get<CompraResumenDto[]>(`${environment.apiUrl}/compras`, {
      params: { ...(desde && { desde }), ...(hasta && { hasta }) },
    });
  }

  resumenCompras(desde?: string, hasta?: string): Observable<ResumenComprasDto> {
    return this.http.get<ResumenComprasDto>(`${environment.apiUrl}/compras/resumen`, {
      params: { ...(desde && { desde }), ...(hasta && { hasta }) },
    });
  }
}
