using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIChatApp.Services;

namespace AIChatApp.Controllers;

[ApiController]
[Route("user")]
public class UserController : ControllerBase
{
    private readonly AuthService _authService;

    public UserController(AuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var user = _authService.GetCurrentUser(User);

        if (user == null)
            return Unauthorized();

        return Ok(user);
    }

    [AllowAnonymous]
    [HttpPost("upgrade")]
    public async Task<IActionResult> UpgradeToPro()
    {
        var result = await _authService.UpgradeToProAsync(User);

        return Ok(new
        {
            success = true,
            data = result
        });
    }
}