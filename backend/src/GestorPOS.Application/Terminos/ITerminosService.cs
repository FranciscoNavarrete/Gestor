namespace GestorPOS.Application.Terminos;

public record TerminosSeccionDto(string Titulo, string Texto);

public record TerminosDto(string Version, IReadOnlyList<string> Resumen, IReadOnlyList<TerminosSeccionDto> Secciones);

/// <summary>Aceptados: el dueño del negocio ya aceptó la versión vigente.</summary>
public record EstadoTerminosDto(string VersionVigente, bool Aceptados, DateTime? AceptadosEnUtc, string? AceptadoPor);

public record AceptacionTerminosDto(string Version, string Origen, string Nombre, DateTime FechaUtc, string? Ip);

public interface ITerminosService
{
    /// <summary>El estado de aceptación del negocio del usuario actual.</summary>
    Task<EstadoTerminosDto> EstadoAsync(CancellationToken ct = default);

    /// <summary>El dueño del negocio acepta la versión vigente. Si ya la había aceptado no hace nada.</summary>
    Task AceptarAsync(string? ip, CancellationToken ct = default);

    /// <summary>Todas las constancias del negocio, de la más nueva a la más vieja (para el operador).</summary>
    Task<IReadOnlyList<AceptacionTerminosDto>> HistorialAsync(Guid tenantId, CancellationToken ct = default);
}
