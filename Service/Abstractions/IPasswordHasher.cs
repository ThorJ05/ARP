using BusinessEsbjerg.Holiday.Domain.Entities;

namespace Service.Abstractions;

public enum PasswordCheck
{
    Failed,
    Success,
    SuccessRehashNeeded // correct password, but the hash is old (e.g. fewer iterations) and should be updated
}

public interface IPasswordHasher
{
    string Hash(User user, string password);

    /// <summary>Checks the password against user.PasswordHash.</summary>
    PasswordCheck Verify(User user, string password);
}
