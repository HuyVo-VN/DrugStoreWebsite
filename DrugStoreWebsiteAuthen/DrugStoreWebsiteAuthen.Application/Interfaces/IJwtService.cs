using System.Threading.Tasks;

namespace DrugStoreWebsiteAuthen.Application.Interfaces;

public interface IJwtService
{
    Task<string> GenerateJwtToken(string userName);
    Task <string> GenerateRefreshToken();
}
