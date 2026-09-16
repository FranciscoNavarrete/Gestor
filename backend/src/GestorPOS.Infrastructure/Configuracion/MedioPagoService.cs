using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Configuracion;
using GestorPOS.Application.Configuracion.Dtos;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Configuracion;

public class MedioPagoService : IMedioPagoService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public MedioPagoService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<MedioPagoDto>> ListarAsync(CancellationToken ct = default)
    {
        return await _db.MediosPago
            .OrderByDescending(m => m.EsProtegido)
            .ThenBy(m => m.Nombre)
            .Select(m => new MedioPagoDto(m.Id, m.Nombre, m.Activo, m.EsProtegido))
            .ToListAsync(ct);
    }

    public async Task<MedioPagoDto> CrearAsync(CrearMedioPagoRequest request, CancellationToken ct = default)
    {
        var medioPago = MedioPagoConfiguracion.Crear(_tenantContext.TenantId, request.Nombre);
        _db.MediosPago.Add(medioPago);
        await _db.SaveChangesAsync(ct);
        return new MedioPagoDto(medioPago.Id, medioPago.Nombre, medioPago.Activo, medioPago.EsProtegido);
    }

    public async Task<MedioPagoDto> EditarAsync(Guid id, EditarMedioPagoRequest request, CancellationToken ct = default)
    {
        var medioPago = await _db.MediosPago.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new AppException("El medio de pago no existe.");

        if (medioPago.EsProtegido)
            throw new AppException("\"Efectivo\" es un medio de pago fijo del sistema y no se puede renombrar.");

        medioPago.Renombrar(request.Nombre);
        await _db.SaveChangesAsync(ct);
        return new MedioPagoDto(medioPago.Id, medioPago.Nombre, medioPago.Activo, medioPago.EsProtegido);
    }

    public async Task DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var medioPago = await _db.MediosPago.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new AppException("El medio de pago no existe.");

        if (medioPago.EsProtegido)
            throw new AppException("\"Efectivo\" es un medio de pago fijo del sistema y no se puede desactivar.");

        medioPago.Desactivar();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> EsValidoYActivoAsync(string nombre, CancellationToken ct = default)
    {
        return await _db.MediosPago.AnyAsync(m => m.Nombre == nombre && m.Activo, ct);
    }
}
