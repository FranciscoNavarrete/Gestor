using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Compras;
using GestorPOS.Application.Compras.Dtos;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Compras;

public class ProveedorService : IProveedorService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ProveedorService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<ProveedorDto>> ListarAsync(CancellationToken ct = default)
    {
        return await _db.Proveedores
            .OrderBy(p => p.Nombre)
            .Select(p => new ProveedorDto(p.Id, p.Nombre, p.Telefono, p.Email, p.Activo))
            .ToListAsync(ct);
    }

    public async Task<ProveedorDto> CrearAsync(CrearProveedorRequest request, CancellationToken ct = default)
    {
        var proveedor = Proveedor.Crear(_tenantContext.TenantId, request.Nombre, request.Telefono, request.Email);
        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync(ct);
        return new ProveedorDto(proveedor.Id, proveedor.Nombre, proveedor.Telefono, proveedor.Email, proveedor.Activo);
    }

    public async Task<ProveedorDto> EditarAsync(Guid id, EditarProveedorRequest request, CancellationToken ct = default)
    {
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("El proveedor no existe.");

        proveedor.Editar(request.Nombre, request.Telefono, request.Email);
        await _db.SaveChangesAsync(ct);
        return new ProveedorDto(proveedor.Id, proveedor.Nombre, proveedor.Telefono, proveedor.Email, proveedor.Activo);
    }

    public async Task DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("El proveedor no existe.");

        proveedor.Desactivar();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ProveedorDto> ActivarAsync(Guid id, CancellationToken ct = default)
    {
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("El proveedor no existe.");

        proveedor.Activar();
        await _db.SaveChangesAsync(ct);
        return new ProveedorDto(proveedor.Id, proveedor.Nombre, proveedor.Telefono, proveedor.Email, proveedor.Activo);
    }
}
