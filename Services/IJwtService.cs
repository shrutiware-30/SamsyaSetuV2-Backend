using G2CCRMPortal.Models;

namespace G2CCRMPortal.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}