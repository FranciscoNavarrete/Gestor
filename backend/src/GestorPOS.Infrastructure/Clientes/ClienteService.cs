using GestorPOS.Application.Clientes;
using GestorPOS.Application.Clientes.Dtos;
using GestorPOS.Application.Common.Dtos;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Common;
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
            .Select(c => new ClienteDto(
                c.Id, c.Telefono, c.Nombre, c.FechaCreacion,
                _db.Ventas.Count(v => v.ClienteId == c.Id),
                (_db.Ventas.Where(v => v.ClienteId == c.Id && v.MedioPago == CuentaCorriente.MedioPago).Sum(v => (decimal?)v.Total) ?? 0m)
                    - (_db.PagosCuenta.Where(p => p.ClienteId == c.Id).Sum(p => (decimal?)p.Monto) ?? 0m)))
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
        var saldoCuentaCorriente = await ObtenerSaldoCuentaCorrienteAsync(id, ct);

        return new ClienteDetalleDto(
            cliente.Id, cliente.Telefono, cliente.Nombre, cliente.FechaCreacion,
            cantidadCompras, totalGastado, ultimaCompra, saldoCuentaCorriente);
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

        var telefonoNormalizado = NormalizarTelefono(request.Telefono);
        if (telefonoNormalizado.Length == 0)
            throw new AppException("El teléfono del cliente es inválido.");

        if (telefonoNormalizado != cliente.Telefono)
        {
            var telefonoEnUso = await _db.Clientes.AnyAsync(c => c.Id != id && c.Telefono == telefonoNormalizado, ct);
            if (telefonoEnUso)
                throw new AppException("Ya existe otro cliente con ese teléfono.");
        }

        cliente.ActualizarNombre(request.Nombre);
        cliente.ActualizarTelefono(telefonoNormalizado);
        await _db.SaveChangesAsync(ct);

        var cantidadCompras = await _db.Ventas.CountAsync(v => v.ClienteId == id, ct);
        var saldoCuentaCorriente = await ObtenerSaldoCuentaCorrienteAsync(id, ct);
        return new ClienteDto(
            cliente.Id, cliente.Telefono, cliente.Nombre, cliente.FechaCreacion, cantidadCompras, saldoCuentaCorriente);
    }

    public async Task<Guid> ObtenerOCrearPorTelefonoAsync(string telefono, string? nombre, CancellationToken ct = default)
    {
        var normalizado = NormalizarTelefono(telefono);
        if (normalizado.Length == 0)
            throw new AppException("El teléfono del cliente es inválido.");

        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Telefono == normalizado, ct);
        if (cliente is not null)
            return cliente.Id;

        cliente = Cliente.Crear(_tenantContext.TenantId, normalizado, nombre);
        _db.Clientes.Add(cliente);
        return cliente.Id;
    }

    public async Task<IReadOnlyList<MovimientoCuentaDto>> ListarMovimientosCuentaAsync(Guid id, CancellationToken ct = default)
    {
        var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == id, ct);
        if (!clienteExiste)
            throw new AppException("El cliente no existe.");

        var fiados = await _db.Ventas
            .Where(v => v.ClienteId == id && v.MedioPago == CuentaCorriente.MedioPago)
            .Select(v => new MovimientoCuentaDto(v.FechaCreacion, "Fiado", v.Total, null))
            .ToListAsync(ct);

        var pagos = await _db.PagosCuenta
            .Where(p => p.ClienteId == id)
            .Select(p => new MovimientoCuentaDto(p.FechaCreacion, "Pago", -p.Monto, p.MedioPago))
            .ToListAsync(ct);

        return fiados.Concat(pagos).OrderByDescending(m => m.Fecha).ToList();
    }

    public async Task<ClienteDetalleDto> RegistrarPagoCuentaAsync(
        Guid id, RegistrarPagoCuentaRequest request, CancellationToken ct = default)
    {
        var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == id, ct);
        if (!clienteExiste)
            throw new AppException("El cliente no existe.");

        if (request.Monto <= 0)
            throw new AppException("El monto a pagar tiene que ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(request.MedioPago))
            throw new AppException("Elegí el medio de pago.");

        var saldoActual = await ObtenerSaldoCuentaCorrienteAsync(id, ct);
        if (request.Monto > saldoActual)
            throw new AppException($"El cliente debe ${saldoActual}, no se puede registrar un pago mayor.");

        var pago = PagoCuenta.Crear(_tenantContext.TenantId, id, request.Monto, request.MedioPago, _tenantContext.UsuarioId);
        _db.PagosCuenta.Add(pago);
        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(id, ct);
    }

    private async Task<decimal> ObtenerSaldoCuentaCorrienteAsync(Guid clienteId, CancellationToken ct)
    {
        var totalACuenta = await _db.Ventas
            .Where(v => v.ClienteId == clienteId && v.MedioPago == CuentaCorriente.MedioPago)
            .SumAsync(v => (decimal?)v.Total, ct) ?? 0m;
        var totalPagado = await _db.PagosCuenta
            .Where(p => p.ClienteId == clienteId)
            .SumAsync(p => (decimal?)p.Monto, ct) ?? 0m;
        return totalACuenta - totalPagado;
    }

    private static string NormalizarTelefono(string telefono) =>
        new(telefono.Where(char.IsDigit).ToArray());
}
