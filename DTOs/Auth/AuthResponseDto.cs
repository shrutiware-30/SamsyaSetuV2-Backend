namespace G2CCRMPortal.DTOs.Auth;

public class AuthResponseDto
{
    public string Status { get; set; } = "success";
    public string Token { get; set; } = null!;
    public UserSummaryDto Data { get; set; } = null!;
}

public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? MobileNumber { get; set; }
    public string Role { get; set; } = null!;
    public int? WardId { get; set; }
}