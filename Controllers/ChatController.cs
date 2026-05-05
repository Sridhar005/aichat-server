using AIChatApp.Models;
using AIChatApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Models;
using System.Security.Claims;

namespace AIChatApp.Controllers;

[ApiController]
[Route("chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(claim))
            throw new UnauthorizedAccessException("User not authenticated");

        return Guid.Parse(claim);
    }


    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Message))
            return BadRequest("Invalid request");

        try
        {
            var reply = await _chatService.SendMessage(
                req.ChatId,
                req.Message,
                GetUserId()
            );

            return Ok(new { reply });
        }
        catch (InvalidOperationException ex) when (ex.Message == "LIMIT_REACHED")
        {
            return StatusCode(429, "Daily message limit reached");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{chatId}/history")]
    public async Task<IActionResult> GetHistory(Guid chatId)
    {
        try
        {
            var history = await _chatService.GetHistory(chatId, GetUserId());
            return Ok(history);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetChats()
    {
        return Ok(await _chatService.GetChats(GetUserId()));
    }

    [HttpPost("new")]
    public async Task<IActionResult> CreateChat()
    {
        return Ok(await _chatService.CreateChat(GetUserId()));
    }
}