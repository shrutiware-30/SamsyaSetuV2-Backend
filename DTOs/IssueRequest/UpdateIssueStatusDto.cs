using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.IssueRequest;

public class UpdateIssueStatusDto
{
    [Required, MaxLength(30)]
    public string NewStatus { get; set; } = null!;

    public string? Remarks { get; set; }
}