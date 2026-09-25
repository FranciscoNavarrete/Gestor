using GestorPOS.Application.Caja;
using GestorPOS.Application.Caja.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/caja")]
[Authorize]
public class CajaController : ControllerBase
{
    private readonly ICajaService _cajaService;

    public CajaController(ICajaService cajaService)
    {
        _cajaService = cajaService;
    }

    [HttpGet("actual")]
    public async Task<ActionResult<CajaDto?>> ObtenerActual(CancellationToken ct)
        => Ok(await _cajaService.ObtenerActualAsync(ct));

    [HttpPost("abrir")]
    public async Task<ActionResult<CajaDto>> Abrir(AbrirCajaRequest request, CancellationToken ct)
        => Ok(await _cajaService.AbrirAsync(request, ct));

    [HttpPost("cerrar")]
    public async Task<ActionResult<CajaDto>> Cerrar(CerrarCajaRequest request, CancellationToken ct)
        => Ok(await _cajaService.CerrarAsync(request, ct));

    [HttpPost("movimientos")]
    public async Task<ActionResult<CajaDto>> AgregarMovimiento(CrearMovimientoCajaRequest request, CancellationToken ct)
        => Ok(await _cajaService.AgregarMovimientoAsync(request, ct));
}
