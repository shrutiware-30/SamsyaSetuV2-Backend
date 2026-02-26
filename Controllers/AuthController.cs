using System.Security.Claims;
using System.Security.Cryptography;
using G2CCRMPortal.Data;
using G2CCRMPortal.DTOs.Auth;
using G2CCRMPortal.Models;
using G2CCRMPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2CCRMPortal.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly G2CCrmDbContext _db;
    private readonly IJwtService _jwt;
    private readonly IEmailService _email;
    private readonly IWebHostEnvironment _env;

    public AuthController(G2CCrmDbContext db, IJwtService jwt, IEmailService email, IWebHostEnvironment env)
    {
        _db = db;
        _jwt = jwt;
        _email = email;
        _env = env;
    }

    // POST api/v1/auth/signup  — mirrors: router.post('/signup', authController.signup)
    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupDto dto)
    {
        // Check if mobile already registered
        if (await _db.Users.AnyAsync(u => u.MobileNumber == dto.MobileNumber && u.IsActive))
            return BadRequest(new { status = "fail", message = "Mobile number already registered." });

        var user = new User
        {
            Name = dto.Name,
            MobileNumber = dto.MobileNumber,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "Citizen",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // TODO: Re-enable when SMTP credentials are configured
        // if (!string.IsNullOrEmpty(user.Email))
        //     await _email.SendWelcomeAsync(user.Email, user.Name);

        var token = _jwt.GenerateToken(user);
        SetTokenCookie(token);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Data = MapUserSummary(user)
        });
    }

    // POST api/v1/auth/create-staff  — admin creates Officer/Admin accounts
    [HttpPost("create-staff")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateStaff([FromBody] AdminSignupDto dto)
    {
        if (dto.Role is not ("Admin" or "Officer"))
            return BadRequest(new { status = "fail", message = "Role must be Admin or Officer." });

        if (dto.Role == "Officer" && dto.WardId is null)
            return BadRequest(new { status = "fail", message = "WardId is required for Officers." });

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.IsActive))
            return BadRequest(new { status = "fail", message = "Email already registered." });

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            WardId = dto.WardId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(null, new { status = "success", data = MapUserSummary(user) });
    }

    // POST api/v1/auth/login  — mirrors: router.post('/login', authController.login)
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        // Find by email or mobile
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.IsActive && (u.Email == dto.EmailOrMobile || u.MobileNumber == dto.EmailOrMobile));

        if (user is null || string.IsNullOrEmpty(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { status = "fail", message = "Incorrect credentials." });
        }

        var token = _jwt.GenerateToken(user);
        SetTokenCookie(token);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Data = MapUserSummary(user)
        });
    }

    // POST api/v1/auth/forgot-password  — mirrors: router.post('/forgotPassword')
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);
        if (user is null)
            return NotFound(new { status = "fail", message = "No user with that email." });

        // Generate reset token (store hash, send plain)
        var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hashedToken = Convert.ToHexString(
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(resetToken)));

        // Store in a temporary claim — in production use a dedicated table or cache
        user.PasswordHash = $"RESET:{hashedToken}:{DateTime.UtcNow.AddMinutes(10):O}|{user.PasswordHash}";
        await _db.SaveChangesAsync();

        try
        {
            await _email.SendPasswordResetAsync(user.Email!, user.Name, resetToken);
            return Ok(new { status = "success", message = "Token sent to email!" });
        }
        catch
        {
            // Rollback token on email failure
            user.PasswordHash = user.PasswordHash.Split('|').Last();
            await _db.SaveChangesAsync();
            return StatusCode(500, new { status = "error", message = "Error sending email. Try again later." });
        }
    }

    // PATCH api/v1/auth/reset-password  — mirrors: router.patch('/resetPassword/:token')
    [HttpPatch("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var hashedToken = Convert.ToHexString(
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(dto.Token)));

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.IsActive && u.PasswordHash != null && u.PasswordHash.Contains($"RESET:{hashedToken}:"));

        if (user is null)
            return BadRequest(new { status = "fail", message = "Token is invalid or has expired." });

        // Validate expiry
        var parts = user.PasswordHash!.Split('|')[0].Split(':');
        if (DateTime.Parse(parts[2]) < DateTime.UtcNow)
            return BadRequest(new { status = "fail", message = "Token has expired." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            Data = MapUserSummary(user)
        });
    }

    // PATCH api/v1/auth/update-password  — mirrors: router.patch('/updateMyPassword')
    [HttpPatch("update-password")]
    [Authorize]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return Unauthorized(new { status = "fail", message = "Current password is incorrect." });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            Data = MapUserSummary(user)
        });
    }

    // GET api/v1/auth/logout  — mirrors: router.get('/logout')
    [HttpGet("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Append("jwt", "loggedout", new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTimeOffset.UtcNow.AddSeconds(2)
        });
        return Ok(new { status = "success" });
    }

    // ── helpers ───────────────────────────────────────────────

    private void SetTokenCookie(string token)
    {
        Response.Cookies.Append("jwt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static UserSummaryDto MapUserSummary(User u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        MobileNumber = u.MobileNumber,
        Role = u.Role,
        WardId = u.WardId
    };
}