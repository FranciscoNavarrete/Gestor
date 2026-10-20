import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ActualizarPreciosMasivoRequest,
  AlcancePreciosMasivo,
  ResultadoPreciosMasivo,
  Categoria,
  CrearCategoriaRequest,
  CrearMarcaRequest,
  CrearPresentacionRequest,
  CrearProductoRequest,
  EditarCategoriaRequest,
  EditarMarcaRequest,
  EditarPresentacionRequest,
  EditarProductoRequest,
  ImportarProductosResultado,
  Marca,
  PaginaProductos,
  Presentacion,
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

  listarMarcas(): Observable<Marca[]> {
    return this.http.get<Marca[]>(`${environment.apiUrl}/marcas`);
  }

  crearMarca(request: CrearMarcaRequest): Observable<Marca> {
    return this.http.post<Marca>(`${environment.apiUrl}/marcas`, request);
  }

  editarMarca(id: string, request: EditarMarcaRequest): Observable<Marca> {
    return this.http.put<Marca>(`${environment.apiUrl}/marcas/${id}`, request);
  }

  desactivarMarca(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/marcas/${id}`);
  }

  eliminarMarca(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/marcas/${id}/permanente`);
  }

  activarMarca(id: string): Observable<Marca> {
    return this.http.post<Marca>(`${environment.apiUrl}/marcas/${id}/activar`, {});
  }

  listarProductos(soloBajoStock = false): Observable<Producto[]> {
    return this.http.get<Producto[]>(`${environment.apiUrl}/productos`, {
      params: soloBajoStock ? { bajoStock: true } : {},
    });
  }

  buscarProductos(
    busqueda: string,
    soloBajoStock: boolean,
    soloInactivos: boolean,
    pagina: number,
    tamanoPagina: number,
  ): Observable<PaginaProductos> {
    return this.http.get<PaginaProductos>(`${environment.apiUrl}/productos/buscar`, {
      params: { busqueda, bajoStock: soloBajoStock, soloInactivos, pagina, tamanoPagina },
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

  crearPresentacion(productoId: string, request: CrearPresentacionRequest): Observable<Presentacion> {
    return this.http.post<Presentacion>(`${environment.apiUrl}/productos/${productoId}/presentaciones`, request);
  }

  editarPresentacion(productoId: string, id: string, request: EditarPresentacionRequest): Observable<Presentacion> {
    return this.http.put<Presentacion>(`${environment.apiUrl}/productos/${productoId}/presentaciones/${id}`, request);
  }

  desactivarPresentacion(productoId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/productos/${productoId}/presentaciones/${id}`);
  }

  actualizarPreciosMasivo(request: ActualizarPreciosMasivoRequest): Observable<ResultadoPreciosMasivo> {
    return this.http.post<ResultadoPreciosMasivo>(`${environment.apiUrl}/productos/actualizar-precios-masivo`, request);
  }

  alcancePreciosMasivo(categoriaId: string | null, marcaId: string | null): Observable<AlcancePreciosMasivo> {
    let params = new HttpParams();
    if (categoriaId) params = params.set('categoriaId', categoriaId);
    if (marcaId) params = params.set('marcaId', marcaId);
    return this.http.get<AlcancePreciosMasivo>(`${environment.apiUrl}/productos/actualizar-precios-masivo/alcance`, { params });
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
