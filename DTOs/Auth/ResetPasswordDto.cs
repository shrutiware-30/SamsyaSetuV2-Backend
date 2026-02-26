using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.Auth;

public class ResetPasswordDto
{
    [Required]
    public string Token { get; set; } = null!;

    [Required, MinLength(8)]
    public string Password { get; set; } = null!;

    [Required, Compare(nameof(Password))]
    public string PasswordConfirm { get; set; } = null!;
}