using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.Auth;

public class ForgotPasswordDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;
}