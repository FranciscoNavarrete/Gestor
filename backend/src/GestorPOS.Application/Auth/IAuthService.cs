using GestorPOS.Application.Auth.Dtos;

namespace GestorPOS.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>El usuario logueado cambia su propia contraseña (también sirve para salir de una temporal).</summary>
    Task CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken ct = default);
}
