using GestorPOS.Application.Clientes;
using GestorPOS.Application.Clientes.Dtos;
using GestorPOS.Application.Common.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorPOS.Infrastructure.Clientes;

public class ClienteService : IClienteService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ClienteService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PaginaDto<ClienteDto>> BuscarAsync(
        string? busqueda, int pagina, int tamanoPagina, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 100);

        var query = _db.Clientes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();
            query = query.Where(c =>
                c.Telefono.Contains(termino) ||
                (c.Nombre != null && c.Nombre.ToLower().Contains(termino)));
        }

        var totalItems = await query.CountAsync(ct);
        var totalPaginas = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)tamanoPagina);

        var items = await query
            .OrderByDescending(c => c.FechaCreacion)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(c => ToDto(c))
            .ToListAsync(ct);

        return new PaginaDto<ClienteDto>(items, pagina, tamanoPagina, totalItems, totalPaginas);
    }

    public async Task<ClienteDetalleDto> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("El cliente no existe.");

        var ventasQuery = _db.Ventas.Where(v => v.ClienteId == id);

        var cantidadCompras = await ventasQuery.CountAsync(ct);
        var totalGastado = await ventasQuery.SumAsync(v => (decimal?)v.Total, ct) ?? 0;
        var ultimaCompra = await ventasQuery
            .OrderByDescending(v => v.FechaCreacion)
            .Select(v => (DateTime?)v.FechaCreacion)
            .FirstOrDefaultAsync(ct);

        return new ClienteDetalleDto(
            cliente.Id, cliente.Telefono, cliente.Nombre, cliente.FechaCreacion,
            cantidadCompras, totalGastado, ultimaCompra);
    }

    public async Task<IReadOnlyList<ClienteVentaDto>> ListarVentasAsync(Guid id, CancellationToken ct = default)
    {
        var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == id, ct);
        if (!clienteExiste)
            throw new AppException("El cliente no existe.");

        return await _db.Ventas
            .Where(v => v.ClienteId == id)
            .OrderByDescending(v => v.FechaCreacion)
            .Select(v => new ClienteVentaDto(v.Id, v.FechaCreacion, v.Total, v.MedioPago))
            .ToListAsync(ct);
    }

    public async Task<ClienteDto> EditarAsync(Guid id, EditarClienteRequest request, CancellationToken ct = default)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("El cliente no existe.");

        cliente.ActualizarNombre(request.Nombre);
        await _db.SaveChangesAsync(ct);

        return ToDto(cliente);
    }

    public async Task<Guid> ObtenerOCrearPorTelefonoAsync(string telefono, CancellationToken ct = default)
    {
        var normalizado = NormalizarTelefono(telefono);
        if (normalizado.Length == 0)
            throw new AppException("El teléfono del cliente es inválido.");

        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Telefono == normalizado, ct);
        if (cliente is not null)
            return cliente.Id;

        cliente = Cliente.Crear(_tenantContext.TenantId, normalizado);
        _db.Clientes.Add(cliente);
        return cliente.Id;
    }

    private static string NormalizarTelefono(string telefono) =>
        new(telefono.Where(char.IsDigit).ToArray());

    private static ClienteDto ToDto(Cliente c) => new(c.Id, c.Telefono, c.Nombre, c.FechaCreacion);
}
