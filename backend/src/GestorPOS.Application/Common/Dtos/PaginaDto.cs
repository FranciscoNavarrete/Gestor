namespace GestorPOS.Application.Common.Dtos;

public record PaginaDto<T>(IReadOnlyList<T> Items, int Pagina, int TamanoPagina, int TotalItems, int TotalPaginas);
