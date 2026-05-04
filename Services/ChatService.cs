using AIChatApp.Data;
using AIChatApp.Models;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;

namespace AIChatApp.Services;

public class ChatService
{
    private readonly AppDbContext _context;
    private readonly GeminiClient _gemini;
    private readonly CacheService _cache;

    public ChatService(
        AppDbContext context,
        GeminiClient gemini,
        CacheService cache)
    {
        _context = context;
        _gemini = gemini;
        _cache = cache;
    }

    public async Task<Chat> CreateChat(Guid userId)
    {
        var chat = new Chat
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "New Chat",
            CreatedAt = DateTime.UtcNow
        };

        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        return chat;
    }

    public async Task<List<Chat>> GetChats(Guid userId)
    {
        return await _context.Chats
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ChatMessage>> GetHistory(Guid chatId, Guid userId)
    {
        bool authorized = await _context.Chats
            .AnyAsync(c => c.Id == chatId && c.UserId == userId);

        if (!authorized)
            throw new UnauthorizedAccessException("Unauthorized chat access");

        string cacheKey = $"chat_history_{chatId}";

        var cached = await _cache.GetAsync<List<ChatMessage>>(cacheKey);
        if (cached != null)
            return cached;

        var messages = await _context.Messages
            .AsNoTracking()
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        await _cache.SetAsync(cacheKey, messages, minutes: 5);

        return messages;
    }

    public async Task<string> SendMessage(Guid chatId, string message, Guid userId)
    {
        var chat = await _context.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId);

        if (chat == null)
            throw new UnauthorizedAccessException("Invalid chat");

        var user = await _context.Users.FindAsync(userId)
            ?? throw new Exception("User not found");

        user.Plan ??= "basic";

        // ✅ Count ONLY USER messages (not AI)
        if (user.Plan == "basic")
        {
            DateTime today = DateTime.UtcNow.Date;

            int todayCount = await _context.Messages.CountAsync(m =>
                m.UserId == userId &&
                m.Sender == "user" &&
                m.Timestamp >= today &&
                m.Timestamp < today.AddDays(1));

            if (todayCount >= 20)
                throw new InvalidOperationException("LIMIT_REACHED");
        }

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            UserId = userId,
            Sender = "user",
            Text = message,
            Timestamp = DateTime.UtcNow
        };

        _context.Messages.Add(userMessage);

        string reply = await _gemini.GetReplyAsync(message);

        var aiMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            UserId = userId,
            Sender = "ai",
            Text = reply,
            Timestamp = DateTime.UtcNow
        };

        _context.Messages.Add(aiMessage);

        await _context.SaveChangesAsync();

        // ✅ Invalidate cache so history updates immediately
        await _cache.RemoveAsync($"chat_history_{chatId}");

        return reply;
    }
}