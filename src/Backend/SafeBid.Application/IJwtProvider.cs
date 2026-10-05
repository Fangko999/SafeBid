using SafeBid.Domain;

namespace SafeBid.Application;

public interface IJwtProvider
{
    string Generate(User user);
}
