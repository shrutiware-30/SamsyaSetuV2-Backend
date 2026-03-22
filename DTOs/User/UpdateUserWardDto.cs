using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.User;

public class UpdateUserWardDto
{
    [Required]
    public int WardId { get; set; }
}