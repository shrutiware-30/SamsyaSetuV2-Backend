using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.Auth;

public class SendOtpDto
{
    [Required]
    public string Identifier { get; set; } = null!;

    [Required, RegularExpression("^(mobile|email)$", ErrorMessage = "Method must be 'mobile' or 'email'.")]
    public string Method { get; set; } = null!;
}