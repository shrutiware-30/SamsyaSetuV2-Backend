using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.Auth;

public class SignupDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(15)]
    public string MobileNumber { get; set; } = null!;

    [EmailAddress, MaxLength(100)]
    public string? Email { get; set; }

    [Required, MinLength(8)]
    public string Password { get; set; } = null!;

    [Required, Compare(nameof(Password))]
    public string PasswordConfirm { get; set; } = null!;
}