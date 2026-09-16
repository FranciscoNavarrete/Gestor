using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Configuracion;
using GestorPOS.Application.Configuracion.Dtos;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Configuracion;

public class NegocioService : INegocioService
{
    private const long TamanoMaximoLogoBytes = 500 * 1024;
    private static readonly HashSet<string> ContentTypesPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg",
    };

    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public NegocioService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<NegocioDto> ObtenerAsync(CancellationToken ct = default)
    {
        var tenant = await ObtenerTenantAsync(ct);
        return ToDto(tenant);
    }

    public async Task<NegocioDto> ActualizarAsync(ActualizarNegocioRequest request, CancellationToken ct = default)
    {
        var tenant = await ObtenerTenantAsync(ct);
        tenant.ActualizarDatos(request.Nombre, request.Telefono);
        await _db.SaveChangesAsync(ct);
        return ToDto(tenant);
    }

    public async Task<NegocioDto> ActualizarLogoAsync(byte[] datos, string contentType, CancellationToken ct = default)
    {
        if (datos.Length == 0)
            throw new AppException("Subí una imagen para el logo.");
        if (datos.Length > TamanoMaximoLogoBytes)
            throw new AppException("El logo no puede pesar más de 500 KB.");
        if (!ContentTypesPermitidos.Contains(contentType))
            throw new AppException("El logo tiene que ser una imagen PNG o JPG.");

        var tenant = await ObtenerTenantAsync(ct);
        tenant.ActualizarLogo(datos, contentType);
        await _db.SaveChangesAsync(ct);
        return ToDto(tenant);
    }

    public async Task<NegocioDto> QuitarLogoAsync(CancellationToken ct = default)
    {
        var tenant = await ObtenerTenantAsync(ct);
        tenant.QuitarLogo();
        await _db.SaveChangesAsync(ct);
        return ToDto(tenant);
    }

    public async Task<(byte[] Datos, string ContentType)?> ObtenerLogoAsync(CancellationToken ct = default)
    {
        var tenant = await ObtenerTenantAsync(ct);
        if (tenant.LogoData is null || tenant.LogoContentType is null) return null;
        return (tenant.LogoData, tenant.LogoContentType);
    }

    private async Task<Tenant> ObtenerTenantAsync(CancellationToken ct)
        => await _db.Tenants.FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, ct)
            ?? throw new AppException("El negocio no existe.");

    private static NegocioDto ToDto(Tenant tenant)
        => new(tenant.Nombre, tenant.Telefono, tenant.LogoData is not null);
}
