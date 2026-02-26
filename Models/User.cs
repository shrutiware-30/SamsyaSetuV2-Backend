using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2CCRMPortal.Models;

[Table("Users")]
public class User
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(15)]
    public string? MobileNumber { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    [Required, MaxLength(20)]
    public string Role { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    public int? WardId { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    // Navigation
    [ForeignKey(nameof(WardId))]
    public Ward? Ward { get; set; }

    public ICollection<IssueRequest> RaisedIssues { get; set; } = [];
    public ICollection<IssueRequest> AssignedIssues { get; set; } = [];
}