using GestorPOS.Application.Auth.Dtos;

namespace GestorPOS.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegistrarNegocioAsync(RegistrarNegocioRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
