namespace PortIA.Core.Services.Common.Security;

public interface IJwtTokenGenerator
{
    JwtTokenResult GenerateToken(int usuarioId, string email, string rol);
}

public record JwtTokenResult(string Token, DateTimeOffset ExpiresAt);
