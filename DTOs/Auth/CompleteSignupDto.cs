using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.Auth;

public class CompleteSignupDto
{
    [Required]
    public string Identifier { get; set; } = null!;

    [Required, RegularExpression("^(mobile|email)$")]
    public string Method { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required, MinLength(8)]
    public string Password { get; set; } = null!;

    [Required, Compare(nameof(Password))]
    public string PasswordConfirm { get; set; } = null!;
}