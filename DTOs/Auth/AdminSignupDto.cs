using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.Auth;

public class AdminSignupDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = null!;

    [Required, MinLength(8)]
    public string Password { get; set; } = null!;

    [Required, Compare(nameof(Password))]
    public string PasswordConfirm { get; set; } = null!;

    [Required]
    public string Role { get; set; } = null!;   // Admin or Officer

    public int? WardId { get; set; }             // Required for Officer
}