using AIChatApp.Data;
using AIChatApp.Models;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;

namespace AIChatApp.Services;

public class ChatService
{
    private readonly AppDbContext _context;
    private readonly GeminiClient _gemini;

    public ChatService(
        AppDbContext context,
        GeminiClient gemini)
    {
        _context = context;
        _gemini = gemini;
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

        var messages = await _context.Messages
            .AsNoTracking()
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return messages;
    }


    public async Task<SendMessageResponse> SendMessage(
    Guid? chatId,
    string message,
    Guid userId)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message required");

        var user = await _context.Users.FindAsync(userId)
            ?? throw new Exception("User not found");

        user.Plan ??= "basic";

        // =========================
        // ✅ BASIC PLAN
        // =========================
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

            // ✅ Gemini reply (string)
            string reply = await _gemini.GetReplyAsync(message);

            // ✅ WRAP STRING INTO RESPONSE OBJECT
            return new SendMessageResponse
            {
                ChatId = Guid.Empty,        // Basic users don’t have chats
                ChatTitle = "Basic Chat",   // Optional label
                Reply = reply
            };
        }

        // =========================
        // ✅ PRO PLAN
        // =========================

        // ✅ Create chat if not provided
        if (!chatId.HasValue || chatId == Guid.Empty)
        {
            var newChat = new Chat
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "New Chat",
                CreatedAt = DateTime.UtcNow
            };

            _context.Chats.Add(newChat);
            await _context.SaveChangesAsync();
            chatId = newChat.Id;
        }

        var chat = await _context.Chats.FirstOrDefaultAsync(c =>
            c.Id == chatId && c.UserId == userId);

        if (chat == null)
            throw new UnauthorizedAccessException("Invalid chat");

        // ✅ Save user message
        _context.Messages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatId = chat.Id,
            UserId = userId,
            Sender = "user",
            Text = message,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // ✅ AI reply
        string replyPro = await _gemini.GetReplyAsync(message);

        // ✅ Generate title only once
        if (chat.Title == "New Chat")
        {
            try
            {
                string title = await _gemini.GenerateChatTitleAsync(message, replyPro);
                chat.Title = title.Length > 200 ? title[..200] : title;
            }
            catch
            {
                // Safe fallback
            }
        }

        // ✅ Save AI message
        _context.Messages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatId = chat.Id,
            UserId = userId,
            Sender = "ai",
            Text = replyPro,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // ✅ Return full response
        return new SendMessageResponse
        {
            ChatId = chat.Id,
            ChatTitle = chat.Title,
            Reply = replyPro
        };
    }
    public async Task DeleteChat(Guid chatId, Guid userId)
    {
        // ✅ Ensure chat belongs to user
        var chat = await _context.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId);

        if (chat == null)
            throw new UnauthorizedAccessException("Chat not found or unauthorized");

        // ✅ Delete messages first
        var messages = _context.Messages.Where(m => m.ChatId == chatId);
        _context.Messages.RemoveRange(messages);

        // ✅ Delete chat
        _context.Chats.Remove(chat);

        await _context.SaveChangesAsync();
    }
}