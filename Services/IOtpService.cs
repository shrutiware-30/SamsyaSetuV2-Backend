namespace G2CCRMPortal.Services;

public interface IOtpService
{
    // Generates a 6-digit OTP, stores it with a 5-minute TTL, and returns the code.
    string Generate(string purpose, string identifier);

    // Returns true if the OTP matches the stored value (consumed on success).
    bool Verify(string purpose, string identifier, string otp);

    // Marks an identifier as OTP-verified for the given purpose.
    void MarkVerified(string purpose, string identifier);

    // Returns true if the identifier was previously verified and consumes the flag.
    bool IsVerified(string purpose, string identifier);
}