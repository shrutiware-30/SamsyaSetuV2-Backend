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
    private readonly ISmsService _sms;
    private readonly IOtpService _otp;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public AuthController(
        G2CCrmDbContext db,
        IJwtService jwt,
        IEmailService email,
        ISmsService sms,
        IOtpService otp,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _email = email;
        _sms = sms;
        _otp = otp;
        _env = env;
        _config = config;
    }

    // ── Existing endpoints ────────────────────────────────────────

    // POST api/v1/auth/signup  — original password-based signup
    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupDto dto)
    {
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
        if (dto.Role == "Officer" && dto.WardId is null)
            return BadRequest(new { status = "fail", message = "WardId is required for Officers." });

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.IsActive))
            return BadRequest(new { status = "fail", message = "Email already registered." });
        User user;
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email && !u.IsActive))
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && !u.IsActive);
            user.Name = dto.Name;
            user.Email = dto.Email;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.Role = dto.Role;
            user.WardId = dto.WardId;
            user.IsActive = true;
            user.CreatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        else
        {
            user = new User
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
        }



        return CreatedAtAction(null, new { status = "success", data = MapUserSummary(user) });
    }

    // POST api/v1/auth/login  — password-based login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.IsActive && (u.Email == dto.EmailOrMobile || u.MobileNumber == dto.EmailOrMobile));

        if (user is null || string.IsNullOrEmpty(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Incorrect credentials." });
        }

        var token = _jwt.GenerateToken(user);
        SetTokenCookie(token);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Data = MapUserSummary(user)
        });
    }

    // POST api/v1/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);
        if (user is null)
            return NotFound(new { message = "No user with that email." });

        var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hashedToken = Convert.ToHexString(
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(resetToken)));

        // Store only token data, not the original password hash
        user.PasswordHash = $"RESET|{hashedToken}|{DateTime.UtcNow.AddMinutes(10):O}";
        await _db.SaveChangesAsync();

        try
        {
            await _email.SendPasswordResetAsync(user.Email!, user.Name, resetToken);
            return Ok(new { status = "success", message = "Token sent to email!" });
        }
        catch
        {
            // Reset to require password reset on next login
            user.PasswordHash = string.Empty;
            await _db.SaveChangesAsync();
            return StatusCode(500, new { message = "Error sending email. Try again later." });
        }
    }

    // PATCH api/v1/auth/reset-password
    [HttpPatch("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var hashedToken = Convert.ToHexString(
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(dto.Token)));

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.IsActive && u.PasswordHash != null && u.PasswordHash.StartsWith("RESET|"));

        if (user is null)
            return BadRequest(new { message = "Token is invalid or has expired." });

        var parts = user.PasswordHash!.Split('|');
        if (parts.Length < 3 || !parts[1].Equals(hashedToken, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Token is invalid." });

        if (DateTime.Parse(parts[2]) < DateTime.UtcNow)
        {
            user.PasswordHash = string.Empty;
            await _db.SaveChangesAsync();
            return BadRequest(new { message = "Token has expired." });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            Data = MapUserSummary(user)
        });
    }

    // PATCH api/v1/auth/update-password
    [HttpPatch("update-password")]
    [Authorize]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return Unauthorized(new { message = "Current password is incorrect." });
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

    // GET api/v1/auth/logout
    [HttpGet("logout")]
    public IActionResult Logout()

    {
        var expirationMinutes = double.Parse(_config["Jwt:ExpiresInMinutes"]!);
        Response.Cookies.Append("jwt", "loggedout", new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
        });
        return Ok(new { status = "success" });
    }

    // ── OTP-based Signup ──────────────────────────────────────────

    // POST api/v1/auth/signup/send-otp
    [HttpPost("signup/send-otp")]
    public async Task<IActionResult> SignupSendOtp([FromBody] SendOtpDto dto)
    {
        // Check if identifier is already registered
        var exists = dto.Method == "mobile"
            ? await _db.Users.AnyAsync(u => u.MobileNumber == dto.Identifier && u.IsActive)
            : await _db.Users.AnyAsync(u => u.Email == dto.Identifier && u.IsActive);

        if (exists)
            return BadRequest(new { message = $"This {dto.Method} is already registered." });

        var otp = _otp.Generate("signup", dto.Identifier);

        if (dto.Method == "mobile")
            await _sms.SendOtpAsync(dto.Identifier, otp);
        else
            await _email.SendOtpAsync(dto.Identifier, otp);
        //Console.Write(otp);
        return Ok(new { status = "success", message = "OTP sent successfully." });
    }

    // POST api/v1/auth/signup/verify-otp
    [HttpPost("signup/verify-otp")]
    public IActionResult SignupVerifyOtp([FromBody] VerifyOtpDto dto)
    {
        if (!_otp.Verify("signup", dto.Identifier, dto.Otp))
            return BadRequest(new { message = "Invalid or expired OTP." });

        _otp.MarkVerified("signup", dto.Identifier);

        return Ok(new { status = "success", message = "OTP verified." });
    }

    // POST api/v1/auth/signup/complete
    [HttpPost("signup/complete")]
    public async Task<IActionResult> SignupComplete([FromBody] CompleteSignupDto dto)
    {

        if (!_otp.IsVerified("signup", dto.Identifier))
            return BadRequest(new { message = "Identifier not verified. Please verify OTP first." });
        User user;
        if (await _db.Users.AnyAsync(u => u.MobileNumber == dto.Identifier && u.Role == "Citizen" && !u.IsActive))
        {
            Console.WriteLine("Enter a MobileNumber");
            user = await _db.Users.FirstOrDefaultAsync(p => p.MobileNumber == dto.Identifier && !p.IsActive);
            user.IsActive = true;
            user.CreatedAt = DateTime.UtcNow;
            user.Name = dto.Name;
            user.MobileNumber = dto.Method == "mobile" ? dto.Identifier : dto.AdditionalContactNumber;
            user.Email = dto.Method == "email" ? dto.Identifier : dto.AdditionalEmail;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.Role = "Citizen";

        }
        else if (await _db.Users.AnyAsync(u => u.Email == dto.Identifier && u.Role == "Citizen" && !u.IsActive))
        {
            user = await _db.Users.FirstOrDefaultAsync(p => p.Email == dto.Identifier && !p.IsActive);
            user.IsActive = true;
            user.CreatedAt = DateTime.UtcNow;
            user.Name = dto.Name;
            user.MobileNumber = dto.Method == "mobile" ? dto.Identifier : dto.AdditionalContactNumber;
            user.Email = dto.Method == "email" ? dto.Identifier : dto.AdditionalEmail;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.Role = "Citizen";

        }


        else
        {
            // Validate additional contact based on signup method


            if (dto.Method == "email" && string.IsNullOrWhiteSpace(dto.AdditionalContactNumber))
                return BadRequest(new { message = "Mobile number is required for email-based signup." });

            if (dto.Method == "mobile" && string.IsNullOrWhiteSpace(dto.AdditionalEmail))
                return BadRequest(new { message = "Email is required for mobile-based signup." });


            // Check if additional contact is already registered
            if (dto.Method == "email")
            {
                if (await _db.Users.AnyAsync(u => u.MobileNumber == dto.AdditionalContactNumber && u.IsActive))
                    return BadRequest(new { message = "Mobile number already registered." });
            }
            else if (dto.Method == "mobile")
            {
                if (await _db.Users.AnyAsync(u => u.Email == dto.AdditionalEmail && u.IsActive))
                    return BadRequest(new { message = "Email already registered." });
            }

            // Prevent duplicate registrations
            var exists = dto.Method == "mobile"
                ? await _db.Users.AnyAsync(u => u.MobileNumber == dto.Identifier && u.IsActive)
                : await _db.Users.AnyAsync(u => u.Email == dto.Identifier && u.IsActive);

            if (exists)
                return BadRequest(new { message = $"This {dto.Method} is already registered." });

            user = new User
            {
                Name = dto.Name,
                MobileNumber = dto.Method == "mobile" ? dto.Identifier : dto.AdditionalContactNumber,
                Email = dto.Method == "email" ? dto.Identifier : dto.AdditionalEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Citizen",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
        }

        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        SetTokenCookie(token);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Data = MapUserSummary(user)
        });
    }



    // ── OTP-based Login ───────────────────────────────────────────

    // POST api/v1/auth/login/send-otp
    [HttpPost("login/send-otp")]
    public async Task<IActionResult> LoginSendOtp([FromBody] SendOtpDto dto)
    {
        var user = dto.Method == "mobile"
            ? await _db.Users.FirstOrDefaultAsync(u => u.MobileNumber == dto.Identifier && u.IsActive)
            : await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Identifier && u.IsActive);

        if (user is null)
            return NotFound(new { message = "No account found with this identifier." });

        var otp = _otp.Generate("login", dto.Identifier);

        if (dto.Method == "mobile")
            await _sms.SendOtpAsync(dto.Identifier, otp);
        else
            await _email.SendOtpAsync(dto.Identifier, otp);

        return Ok(new { status = "success", message = "OTP sent successfully." });
    }

    // POST api/v1/auth/login/verify-otp
    [HttpPost("login/verify-otp")]
    public async Task<IActionResult> LoginVerifyOtp([FromBody] VerifyOtpDto dto)
    {
        if (!_otp.Verify("login", dto.Identifier, dto.Otp))
            return BadRequest(new { message = "Invalid or expired OTP." });

        var user = dto.Method == "mobile"
            ? await _db.Users.FirstOrDefaultAsync(u => u.MobileNumber == dto.Identifier && u.IsActive)
            : await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Identifier && u.IsActive);

        if (user is null)
            return NotFound(new { message = "No account found with this identifier." });

        var token = _jwt.GenerateToken(user);
        SetTokenCookie(token);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Data = MapUserSummary(user)
        });
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void SetTokenCookie(string token)
    {
        var expirationMinutes = double.Parse(_config["Jwt:ExpiresInMinutes"]!);
        Response.Cookies.Append("jwt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
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
