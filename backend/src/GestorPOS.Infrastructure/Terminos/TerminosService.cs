using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Terminos;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Terminos;

public class TerminosService : ITerminosService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public TerminosService(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<EstadoTerminosDto> EstadoAsync(CancellationToken ct = default)
    {
        var aceptacion = await AceptacionDelClienteAsync(_tenant.TenantId, ct);
        return new EstadoTerminosDto(TerminosVigentes.Version, aceptacion is not null, aceptacion?.FechaUtc, aceptacion?.UsuarioNombre);
    }

    public async Task AceptarAsync(string? ip, CancellationToken ct = default)
    {
        if (await AceptacionDelClienteAsync(_tenant.TenantId, ct) is not null) return;

        _db.AceptacionesTerminos.Add(AceptacionTerminos.Registrar(
            _tenant.TenantId, TerminosVigentes.Version, OrigenAceptacionTerminos.Cliente,
            _tenant.UsuarioId, _tenant.UsuarioNombre, ip));
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AceptacionTerminosDto>> HistorialAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.AceptacionesTerminos.AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.FechaUtc)
            .Select(a => new AceptacionTerminosDto(a.Version, a.Origen.ToString(), a.UsuarioNombre, a.FechaUtc, a.Ip))
            .ToListAsync(ct);

    private Task<AceptacionTerminos?> AceptacionDelClienteAsync(Guid tenantId, CancellationToken ct)
        => _db.AceptacionesTerminos.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.Origen == OrigenAceptacionTerminos.Cliente && a.Version == TerminosVigentes.Version)
            .OrderByDescending(a => a.FechaUtc)
            .FirstOrDefaultAsync(ct);
}
