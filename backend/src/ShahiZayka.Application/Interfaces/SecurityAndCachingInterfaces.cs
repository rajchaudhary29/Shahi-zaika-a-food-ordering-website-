using ShahiZayka.Domain.Entities;

namespace ShahiZayka.Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
}

public interface IPasswordHasher
{
    (string Hash, string Salt) HashPassword(string password);
    bool VerifyPassword(string password, string hash, string salt);
}

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, out DateTime expiresAtUtc);
}
