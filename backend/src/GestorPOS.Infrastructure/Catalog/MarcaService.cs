using GestorPOS.Application.Catalog;
using GestorPOS.Application.Catalog.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Catalog;

public class MarcaService : IMarcaService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public MarcaService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<MarcaDto>> ListarAsync(CancellationToken ct = default)
    {
        return await _db.Marcas
            .OrderBy(m => m.Nombre)
            .Select(m => new MarcaDto(m.Id, m.Nombre, m.Activo))
            .ToListAsync(ct);
    }

    public async Task<MarcaDto> CrearAsync(CrearMarcaRequest request, CancellationToken ct = default)
    {
        var marca = Marca.Crear(_tenantContext.TenantId, request.Nombre);
        await ValidarNombreLibreAsync(marca.Nombre, null, ct);
        _db.Marcas.Add(marca);
        await _db.SaveChangesAsync(ct);
        return ToDto(marca);
    }

    public async Task<MarcaDto> EditarAsync(Guid id, EditarMarcaRequest request, CancellationToken ct = default)
    {
        var marca = await _db.Marcas.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new AppException("La marca no existe.");

        marca.Renombrar(request.Nombre);
        await ValidarNombreLibreAsync(marca.Nombre, id, ct);
        await _db.SaveChangesAsync(ct);
        return ToDto(marca);
    }

    public async Task DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var marca = await _db.Marcas.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new AppException("La marca no existe.");

        marca.Desactivar();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<MarcaDto> ActivarAsync(Guid id, CancellationToken ct = default)
    {
        var marca = await _db.Marcas.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new AppException("La marca no existe.");

        marca.Activar();
        await _db.SaveChangesAsync(ct);
        return ToDto(marca);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var marca = await _db.Marcas.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new AppException("La marca no existe.");

        var enUso = await _db.Productos.AnyAsync(p => p.MarcaId == id, ct);
        if (enUso)
            throw new AppException($"\"{marca.Nombre}\" ya se usó en algún producto — no se puede eliminar del todo. Probá desactivarla.");

        _db.Marcas.Remove(marca);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Mensaje claro en vez de dejar que salte el índice único ("Royal Canin" y "royal canin" son la misma).</summary>
    private async Task ValidarNombreLibreAsync(string nombre, Guid? excluirId, CancellationToken ct)
    {
        var minuscula = nombre.ToLower();
        var repetida = await _db.Marcas.AnyAsync(m => m.Id != excluirId && m.Nombre.ToLower() == minuscula, ct);
        if (repetida)
            throw new AppException($"Ya hay una marca llamada '{nombre}'.");
    }

    private static MarcaDto ToDto(Marca m) => new(m.Id, m.Nombre, m.Activo);
}
