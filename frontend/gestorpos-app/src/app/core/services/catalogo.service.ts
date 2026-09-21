import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ActualizarPreciosMasivoRequest,
  Categoria,
  CrearCategoriaRequest,
  CrearProductoRequest,
  EditarCategoriaRequest,
  EditarProductoRequest,
  ImportarProductosResultado,
  PaginaProductos,
  Producto,
} from '../models/catalog.models';

@Injectable({ providedIn: 'root' })
export class CatalogoService {
  constructor(private readonly http: HttpClient) {}

  listarCategorias(): Observable<Categoria[]> {
    return this.http.get<Categoria[]>(`${environment.apiUrl}/categorias`);
  }

  crearCategoria(request: CrearCategoriaRequest): Observable<Categoria> {
    return this.http.post<Categoria>(`${environment.apiUrl}/categorias`, request);
  }

  editarCategoria(id: string, request: EditarCategoriaRequest): Observable<Categoria> {
    return this.http.put<Categoria>(`${environment.apiUrl}/categorias/${id}`, request);
  }

  desactivarCategoria(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/categorias/${id}`);
  }

  eliminarCategoria(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/categorias/${id}/permanente`);
  }

  activarCategoria(id: string): Observable<Categoria> {
    return this.http.post<Categoria>(`${environment.apiUrl}/categorias/${id}/activar`, {});
  }

  listarProductos(soloBajoStock = false): Observable<Producto[]> {
    return this.http.get<Producto[]>(`${environment.apiUrl}/productos`, {
      params: soloBajoStock ? { bajoStock: true } : {},
    });
  }

  buscarProductos(
    busqueda: string,
    soloBajoStock: boolean,
    incluirInactivos: boolean,
    pagina: number,
    tamanoPagina: number,
  ): Observable<PaginaProductos> {
    return this.http.get<PaginaProductos>(`${environment.apiUrl}/productos/buscar`, {
      params: { busqueda, bajoStock: soloBajoStock, incluirInactivos, pagina, tamanoPagina },
    });
  }

  crearProducto(request: CrearProductoRequest): Observable<Producto> {
    return this.http.post<Producto>(`${environment.apiUrl}/productos`, request);
  }

  editarProducto(id: string, request: EditarProductoRequest): Observable<Producto> {
    return this.http.put<Producto>(`${environment.apiUrl}/productos/${id}`, request);
  }

  desactivarProducto(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/productos/${id}`);
  }

  activarProducto(id: string): Observable<Producto> {
    return this.http.post<Producto>(`${environment.apiUrl}/productos/${id}/activar`, {});
  }

  ajustarStock(id: string, cantidad: number, motivo: string): Observable<Producto> {
    return this.http.post<Producto>(`${environment.apiUrl}/productos/${id}/ajustar-stock`, { cantidad, motivo });
  }

  actualizarPreciosMasivo(request: ActualizarPreciosMasivoRequest): Observable<{ productosActualizados: number }> {
    return this.http.post<{ productosActualizados: number }>(
      `${environment.apiUrl}/productos/actualizar-precios-masivo`,
      request,
    );
  }

  descargarPlantillaExcel(): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/productos/plantilla-excel`, { responseType: 'blob' });
  }

  importarExcel(archivo: File): Observable<ImportarProductosResultado> {
    const formData = new FormData();
    formData.append('archivo', archivo);
    return this.http.post<ImportarProductosResultado>(`${environment.apiUrl}/productos/importar-excel`, formData);
  }
}
