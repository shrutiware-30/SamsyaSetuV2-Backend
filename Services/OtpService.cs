using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace G2CCRMPortal.Services;

public class OtpService : IOtpService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);

    public OtpService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string Generate(string purpose, string identifier)
    {
        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var key = CacheKey(purpose, identifier);
        _cache.Set(key, otp, OtpTtl);
        return otp;
    }

    public bool Verify(string purpose, string identifier, string otp)
    {
        var key = CacheKey(purpose, identifier);
        if (!_cache.TryGetValue(key, out string? stored) || stored != otp)
            return false;

        _cache.Remove(key); // single-use
        return true;
    }

    public void MarkVerified(string purpose, string identifier)
    {
        var key = $"verified:{purpose}:{identifier}";
        _cache.Set(key, true, OtpTtl);
    }

    public bool IsVerified(string purpose, string identifier)
    {
        var key = $"verified:{purpose}:{identifier}";
        if (!_cache.TryGetValue(key, out bool verified) || !verified)
            return false;

        _cache.Remove(key); // single-use
        return true;
    }

    private static string CacheKey(string purpose, string identifier) =>
        $"otp:{purpose}:{identifier}";
}