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

    // ✅ OTP itself is single-use (CORRECT)
    public bool Verify(string purpose, string identifier, string otp)
    {
        var key = CacheKey(purpose, identifier);

        if (!_cache.TryGetValue(key, out string? stored) || stored != otp)
            return false;

        _cache.Remove(key); // ✅ OTP consumed once
        return true;
    }

    // ✅ Mark OTP as verified for signup flow
    public void MarkVerified(string purpose, string identifier)
    {
        var key = VerifiedKey(purpose, identifier);
        _cache.Set(key, true, OtpTtl);
    }

    // ✅ FIX: Do NOT remove verification here
    public bool IsVerified(string purpose, string identifier)
    {
        var key = VerifiedKey(purpose, identifier);

        return _cache.TryGetValue(key, out bool verified) && verified;
    }

    private static string CacheKey(string purpose, string identifier) =>
        $"otp:{purpose}:{identifier}";

    private static string VerifiedKey(string purpose, string identifier) =>
        $"verified:{purpose}:{identifier}";
}