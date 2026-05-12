using AIChatApp.Models;
using AIChatApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using System.Security.Claims;
using static MyApp.Models.AuthRequests;

namespace AIChatApp.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly TokenService _tokenService;

    public AuthController(AuthService authService, TokenService tokenService)
    {
        _authService = authService;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(Register request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RegisterAsync(request);

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    //[AllowAnonymous]
    //[HttpGet("me")]
    //public IActionResult Me()
    //{
    //    var token = Request.Cookies["AuthToken"];

    //    if (string.IsNullOrEmpty(token))
    //        return Unauthorized();

    //    var principal = _tokenService.ValidateToken(token);

    //    if (principal == null)
    //        return Unauthorized();

    //    var email = principal.FindFirst(ClaimTypes.Email)?.Value;

    //    return Ok(new
    //    {
    //        email
    //    });
    //}

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(Login request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(request, Response);

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["RefreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized("Refresh token missing");

        var result = await _authService.RefreshTokenAsync(refreshToken, Response);

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["RefreshToken"] ?? string.Empty; ;

        await _authService.Logout(Response, refreshToken);

        return Ok(new
        {
            success = true,
            message = "Logged out"
        });
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request.Email);
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(
            request.Token,
            request.NewPassword
        );

        return Ok(new
        {
            success = true,
            message = "Password has been reset"
        });
    }


}