using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.User;

public class UpdateMeDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [EmailAddress, MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(15)]
    public string? MobileNumber { get; set; }
}