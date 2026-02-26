using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.Auth;

public class LoginDto
{
    [Required]
    public string EmailOrMobile { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}