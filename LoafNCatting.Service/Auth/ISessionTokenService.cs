using LoafNCatting.Data.Models;

namespace LoafNCatting.Service.Auth;

public interface ISessionTokenService
{
    string IssueToken(User user);
    UserSession? GetSession(string token);
    void Revoke(string token);
}
