import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CrearTareaRequest, EditarTareaRequest, TareaDto } from '../models/tarea.models';

@Injectable({ providedIn: 'root' })
export class TareaService {
  constructor(private readonly http: HttpClient) {}

  listar(incluirCompletadas = true): Observable<TareaDto[]> {
    return this.http.get<TareaDto[]>(`${environment.apiUrl}/tareas`, { params: { incluirCompletadas } });
  }

  crear(request: CrearTareaRequest): Observable<TareaDto> {
    return this.http.post<TareaDto>(`${environment.apiUrl}/tareas`, request);
  }

  editar(id: string, request: EditarTareaRequest): Observable<TareaDto> {
    return this.http.put<TareaDto>(`${environment.apiUrl}/tareas/${id}`, request);
  }

  completar(id: string): Observable<TareaDto> {
    return this.http.post<TareaDto>(`${environment.apiUrl}/tareas/${id}/completar`, {});
  }

  marcarPendiente(id: string): Observable<TareaDto> {
    return this.http.post<TareaDto>(`${environment.apiUrl}/tareas/${id}/pendiente`, {});
  }

  eliminar(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/tareas/${id}`);
  }
}
