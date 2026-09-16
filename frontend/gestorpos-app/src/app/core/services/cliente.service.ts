import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Cliente,
  ClienteDetalle,
  ClienteVenta,
  EditarClienteRequest,
  PaginaClientes,
} from '../models/cliente.models';

@Injectable({ providedIn: 'root' })
export class ClienteService {
  constructor(private readonly http: HttpClient) {}

  buscar(busqueda: string, pagina: number, tamanoPagina: number): Observable<PaginaClientes> {
    return this.http.get<PaginaClientes>(`${environment.apiUrl}/clientes`, {
      params: { busqueda, pagina, tamanoPagina },
    });
  }

  obtener(id: string): Observable<ClienteDetalle> {
    return this.http.get<ClienteDetalle>(`${environment.apiUrl}/clientes/${id}`);
  }

  listarVentas(id: string): Observable<ClienteVenta[]> {
    return this.http.get<ClienteVenta[]>(`${environment.apiUrl}/clientes/${id}/ventas`);
  }

  editar(id: string, request: EditarClienteRequest): Observable<Cliente> {
    return this.http.put<Cliente>(`${environment.apiUrl}/clientes/${id}`, request);
  }
}
