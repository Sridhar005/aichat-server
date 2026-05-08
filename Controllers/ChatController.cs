using AIChatApp.Models;
using AIChatApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Models;
using System.Security.Claims;

namespace AIChatApp.Controllers;

[ApiController]
[Route("chat")]
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



    [AllowAnonymous]
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            SendMessageResponse result =
                await _chatService.SendMessage(
                    req.ChatId,
                    req.Message,
                    GetUserId()
                );

            // ✅ Return strongly‑typed response DTO
            return Ok(result);
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


    [AllowAnonymous]
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

    [AllowAnonymous]
    [HttpGet("list")]
    public async Task<IActionResult> GetChats()
    {
        return Ok(await _chatService.GetChats(GetUserId()));
    }

    [AllowAnonymous]
    [HttpPost("new")]
    public async Task<IActionResult> CreateChat()
    {
        return Ok(await _chatService.CreateChat(GetUserId()));
    }


    [AllowAnonymous]
    [HttpDelete("{chatId}")]
    public async Task<IActionResult> DeleteChat(Guid chatId)
    {
        try
        {
            await _chatService.DeleteChat(chatId, GetUserId());
            return NoContent(); // ✅ 204
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

}