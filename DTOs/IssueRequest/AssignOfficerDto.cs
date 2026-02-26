using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.IssueRequest;

public class AssignOfficerDto
{
    [Required]
    public Guid OfficerId { get; set; }
}