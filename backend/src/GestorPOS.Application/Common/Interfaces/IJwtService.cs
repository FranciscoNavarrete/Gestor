using GestorPOS.Domain.Entities;

namespace GestorPOS.Application.Common.Interfaces;

public interface IJwtService
{
    (string Token, DateTime ExpiraUtc) GenerarToken(Usuario usuario, IEnumerable<string> featuresHabilitadas);
}
