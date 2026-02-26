using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.Auth;

public class UpdatePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = null!;

    [Required, Compare(nameof(NewPassword))]
    public string PasswordConfirm { get; set; } = null!;
}