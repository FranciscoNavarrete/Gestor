using GestorPOS.Application.Compras;
using GestorPOS.Application.Compras.Dtos;
using GestorPOS.Domain.Common;
using GestorPOS.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorPOS.WebAPI.Controllers;

[ApiController]
[Route("api/proveedores")]
[Authorize]
[RequireFeature(ComprasFeature.Clave, FacturasProveedorFeature.Clave)]
public class ProveedoresController : ControllerBase
{
    private readonly IProveedorService _proveedorService;

    public ProveedoresController(IProveedorService proveedorService)
    {
        _proveedorService = proveedorService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProveedorDto>>> Listar(CancellationToken ct)
        => Ok(await _proveedorService.ListarAsync(ct));

    [HttpPost]
    public async Task<ActionResult<ProveedorDto>> Crear(CrearProveedorRequest request, CancellationToken ct)
        => Ok(await _proveedorService.CrearAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProveedorDto>> Editar(Guid id, EditarProveedorRequest request, CancellationToken ct)
        => Ok(await _proveedorService.EditarAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken ct)
    {
        await _proveedorService.DesactivarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activar")]
    public async Task<ActionResult<ProveedorDto>> Activar(Guid id, CancellationToken ct)
        => Ok(await _proveedorService.ActivarAsync(id, ct));
}
