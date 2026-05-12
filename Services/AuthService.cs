using AIChatApp.Data;
using AIChatApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AIChatApp.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;
    private readonly IEmailService _emailService;

    public AuthService(AppDbContext context, TokenService tokenService, EmailService emailService)
    {
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
    }
    public async Task<object> RegisterAsync(Register request)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
            throw new Exception("Email already exists");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Plan = "basic"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // 🔐 Generate tokens
        var accessToken = _tokenService.GenerateToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            Revoked = false
        });

        await _context.SaveChangesAsync();

        return new
        {
            message = "User registered successfully",
            user = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Plan
            },
            accessToken,
            refreshToken
        };
    }

    // =========================
    // LOGIN USER
    // =========================
    public object? GetCurrentUser(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return null;

        if (!Guid.TryParse(userId, out var guid))
            return null;

        var dbUser = _context.Users
            .Where(x => x.Id == guid)   // ✅ Guid == Guid
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Email,
                x.Plan
            })
            .FirstOrDefault();

        return dbUser;
    }

    public async Task<object> UpgradeToProAsync(ClaimsPrincipal userClaims)
    {
        var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new Exception("UserId not found");

        if (!Guid.TryParse(userId, out var guid))
            throw new Exception("Invalid UserId format");

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == guid); // ✅ FIXED

        if (user == null)
            throw new Exception("User not found");

        user.Plan = "pro";

        await _context.SaveChangesAsync();

        return new
        {
            user.Id,
            user.Plan
        };
    }
    public async Task<LoginResponse> LoginAsync(Login request, HttpResponse response)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null)
            throw new Exception("Invalid email or password");

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
            throw new Exception("Invalid email or password");

        // 🎟️ Tokens
        var accessToken = _tokenService.GenerateToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            Revoked = false
        });


        await _context.SaveChangesAsync();

        // 🍪 Set cookies (USED by your auth system)
        response.Cookies.Append("AuthToken", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // localhost
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddHours(1)
        });

        response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        // ✅ RETURN PROPER DTO
        return new LoginResponse
        {

            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Plan = user.Plan
        };
    }
    public async Task<object> RefreshTokenAsync(string refreshToken, HttpResponse response)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new Exception("Refresh token missing");

        var tokenRecord = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (tokenRecord == null)
            throw new Exception("Invalid refresh token");

        if (tokenRecord.Revoked)
            throw new Exception("Refresh token revoked");

        if (tokenRecord.ExpiresAt < DateTime.UtcNow)
            throw new Exception("Refresh token expired");

        var user = await _context.Users.FindAsync(tokenRecord.UserId);

        if (user == null)
            throw new Exception("User not found");
        var newAccessToken = _tokenService.GenerateToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        tokenRecord.Revoked = true;

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            Revoked = false
        });

        await _context.SaveChangesAsync();

        SetAuthCookies(response, newAccessToken, newRefreshToken);

        return new
        {
            message = "Token refreshed",
            accessToken = newAccessToken,
            refreshToken = newRefreshToken
        };
    }
    public async Task Logout(HttpResponse response, string refreshToken)
    {
        // 🔥 revoke refresh token in DB
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (token != null)
        {
            token.Revoked = true;
            await _context.SaveChangesAsync();
        }

        // 🧹 delete cookies
        response.Cookies.Delete("AuthToken");
        response.Cookies.Delete("RefreshToken");
    }

    private void SetAuthCookies(HttpResponse response, string accessToken, string refreshToken)
    {
        response.Cookies.Append("AuthToken", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(15) // 🔥 match JWT expiry
        });

        response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        });
    }

    public async Task ForgotPasswordAsync(string email)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user == null)
        return; // prevent email enumeration

    var token = Guid.NewGuid().ToString("N");

    await _emailService.SendAsync(
        user.Email,
        "Reset Password",
        $"Your reset token is: {token}"
    );
}

public async Task ResetPasswordAsync(string token, string newPassword)
{
    // Placeholder implementation (no SendGrid / no DB tokens)
    // You can extend later when you add a real provider again
    await Task.CompletedTask;
}

}
