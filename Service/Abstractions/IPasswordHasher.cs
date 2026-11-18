using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

public enum PasswordCheck
{
    Failed,
    Success,
    
    SuccessRehashNeeded //  correct password, bu t the hsh is old (e.g. fewer iterations) and should be updated
}

public interface IPasswordHasher
{
    string Hash(User user, string password);

    
    // Checks the password against user.PasswordHash.
    PasswordCheck Verify(User user, string password);
}
