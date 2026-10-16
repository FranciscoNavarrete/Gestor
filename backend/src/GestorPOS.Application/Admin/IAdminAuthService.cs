using GestorPOS.Application.Admin.Dtos;

namespace GestorPOS.Application.Admin;

public interface IAdminAuthService
{
    Task<AdminLoginResponse> LoginAsync(AdminLoginRequest request, CancellationToken ct = default);

    /// <summary>El admin logueado (operador o vendedor) cambia su propia contraseña.</summary>
    Task CambiarPasswordAsync(GestorPOS.Application.Auth.Dtos.CambiarPasswordRequest request, CancellationToken ct = default);
}
