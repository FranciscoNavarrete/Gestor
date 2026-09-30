using GestorPOS.Application.Admin.Dtos;

namespace GestorPOS.Application.Admin;

public interface IAdminAuthService
{
    Task<AdminLoginResponse> LoginAsync(AdminLoginRequest request, CancellationToken ct = default);
}
