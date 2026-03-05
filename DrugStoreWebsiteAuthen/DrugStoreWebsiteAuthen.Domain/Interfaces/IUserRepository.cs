using DrugStoreWebsiteAuthen.Domain;

namespace DrugStoreWebsiteAuthen.Domain;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
}