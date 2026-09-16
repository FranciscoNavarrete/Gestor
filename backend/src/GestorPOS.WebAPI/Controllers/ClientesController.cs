using GestorPOS.Application.Clientes;
using GestorPOS.Application.Clientes.Dtos;
using GestorPOS.Application.Common.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClientesController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginaDto<ClienteDto>>> Buscar(
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken ct = default)
        => Ok(await _clienteService.BuscarAsync(busqueda, pagina, tamanoPagina, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClienteDetalleDto>> Obtener(Guid id, CancellationToken ct)
        => Ok(await _clienteService.ObtenerAsync(id, ct));

    [HttpGet("{id:guid}/ventas")]
    public async Task<ActionResult<IReadOnlyList<ClienteVentaDto>>> ListarVentas(Guid id, CancellationToken ct)
        => Ok(await _clienteService.ListarVentasAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClienteDto>> Editar(Guid id, EditarClienteRequest request, CancellationToken ct)
        => Ok(await _clienteService.EditarAsync(id, request, ct));
}
