using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.Auth;

public class VerifyOtpDto
{
    [Required]
    public string Identifier { get; set; } = null!;

    [Required, RegularExpression("^(mobile|email)$")]
    public string Method { get; set; } = null!;

    [Required, StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = null!;
}