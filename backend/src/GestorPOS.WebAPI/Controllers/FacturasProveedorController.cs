using GestorPOS.Application.FacturasProveedor;
using GestorPOS.Application.FacturasProveedor.Dtos;
using GestorPOS.Domain.Common;
using GestorPOS.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/facturas-proveedor")]
[Authorize]
[RequireFeature(FacturasProveedorFeature.Clave)]
public class FacturasProveedorController : ControllerBase
{
    private readonly IFacturaProveedorService _facturaProveedorService;

    public FacturasProveedorController(IFacturaProveedorService facturaProveedorService)
    {
        _facturaProveedorService = facturaProveedorService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FacturaProveedorDto>>> Listar(
        [FromQuery] Guid? proveedorId, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromQuery] bool incluirPagadas, CancellationToken ct)
        => Ok(await _facturaProveedorService.ListarAsync(proveedorId, desde, hasta, incluirPagadas, ct));

    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenFacturasProveedorDto>> Resumen(CancellationToken ct)
        => Ok(await _facturaProveedorService.ResumenAsync(ct));

    [HttpPost]
    public async Task<ActionResult<FacturaProveedorDto>> Crear(CrearFacturaProveedorRequest request, CancellationToken ct)
        => Ok(await _facturaProveedorService.CrearAsync(request, ct));

    [HttpPost("{id:guid}/pagos")]
    public async Task<ActionResult<FacturaProveedorDto>> RegistrarPago(Guid id, RegistrarPagoFacturaRequest request, CancellationToken ct)
        => Ok(await _facturaProveedorService.RegistrarPagoAsync(id, request, ct));
}
