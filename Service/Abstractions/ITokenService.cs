using BusinessEsbjerg.Holiday.Domain.Entities;

namespace Service.Abstractions;

public record AuthToken(string Token, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AuthToken CreateToken(User user);
}
