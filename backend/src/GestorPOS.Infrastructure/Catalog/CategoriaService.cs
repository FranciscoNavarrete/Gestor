using GestorPOS.Application.Catalog;
using GestorPOS.Application.Catalog.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Catalog;

public class CategoriaService : ICategoriaService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CategoriaService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync(CancellationToken ct = default)
    {
        return await _db.Categorias
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaDto(c.Id, c.Nombre, c.Activo))
            .ToListAsync(ct);
    }

    public async Task<CategoriaDto> CrearAsync(CrearCategoriaRequest request, CancellationToken ct = default)
    {
        var categoria = Categoria.Crear(_tenantContext.TenantId, request.Nombre);
        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync(ct);
        return new CategoriaDto(categoria.Id, categoria.Nombre, categoria.Activo);
    }

    public async Task<CategoriaDto> EditarAsync(Guid id, EditarCategoriaRequest request, CancellationToken ct = default)
    {
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("La categoría no existe.");

        categoria.Renombrar(request.Nombre);
        await _db.SaveChangesAsync(ct);
        return new CategoriaDto(categoria.Id, categoria.Nombre, categoria.Activo);
    }

    public async Task DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("La categoría no existe.");

        categoria.Desactivar();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CategoriaDto> ActivarAsync(Guid id, CancellationToken ct = default)
    {
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("La categoría no existe.");

        categoria.Activar();
        await _db.SaveChangesAsync(ct);
        return new CategoriaDto(categoria.Id, categoria.Nombre, categoria.Activo);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("La categoría no existe.");

        var enUso = await _db.Productos.AnyAsync(p => p.CategoriaId == id, ct);
        if (enUso)
            throw new AppException($"\"{categoria.Nombre}\" ya se usó en algún producto — no se puede eliminar del todo. Probá desactivarla.");

        _db.Categorias.Remove(categoria);
        await _db.SaveChangesAsync(ct);
    }
}
