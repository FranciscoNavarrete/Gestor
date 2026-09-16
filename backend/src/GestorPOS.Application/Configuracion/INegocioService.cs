using GestorPOS.Application.Configuracion.Dtos;

namespace GestorPOS.Application.Configuracion;

public interface INegocioService
{
    Task<NegocioDto> ObtenerAsync(CancellationToken ct = default);
    Task<NegocioDto> ActualizarAsync(ActualizarNegocioRequest request, CancellationToken ct = default);
    Task<NegocioDto> ActualizarLogoAsync(byte[] datos, string contentType, CancellationToken ct = default);
    Task<NegocioDto> QuitarLogoAsync(CancellationToken ct = default);
    Task<(byte[] Datos, string ContentType)?> ObtenerLogoAsync(CancellationToken ct = default);
}
