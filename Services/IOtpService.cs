namespace G2CCRMPortal.Services;

public interface IOtpService
{
    /// <summary>Generates a 6-digit OTP, stores it with a 5-minute TTL, and returns the code.</summary>
    string Generate(string purpose, string identifier);

    /// <summary>Returns true if the OTP matches the stored value (consumed on success).</summary>
    bool Verify(string purpose, string identifier, string otp);

    /// <summary>Marks an identifier as OTP-verified for the given purpose.</summary>
    void MarkVerified(string purpose, string identifier);

    /// <summary>Returns true if the identifier was previously verified and consumes the flag.</summary>
    bool IsVerified(string purpose, string identifier);
}