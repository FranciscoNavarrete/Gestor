using GestorPOS.Application.Ventas;
using GestorPOS.Application.Ventas.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/ventas")]
[Authorize]
public class VentasController : ControllerBase
{
    private readonly IVentaService _ventaService;

    public VentasController(IVentaService ventaService)
    {
        _ventaService = ventaService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VentaResumenDto>>> Listar(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
        => Ok(await _ventaService.ListarAsync(desde, hasta, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VentaDto>> Obtener(Guid id, CancellationToken ct)
        => Ok(await _ventaService.ObtenerAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<VentaDto>> Crear(CrearVentaRequest request, CancellationToken ct)
        => Ok(await _ventaService.CrearAsync(request, ct));
}
