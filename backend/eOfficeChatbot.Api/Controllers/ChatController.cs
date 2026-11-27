using System.Text;
using System.Text.Json;
using eOfficeChatbot.Api.Data;
using eOfficeChatbot.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eOfficeChatbot.Api.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession([FromBody] CreateChatSessionRequest request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });

            var session = new ChatSession
            {
                UserId = userId.Value,
                Title = string.IsNullOrWhiteSpace(request.Title)
                    ? "Cuộc trò chuyện mới"
                    : request.Title.Trim()
            };
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok(ToSessionDto(session));
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });

            var sessions = await _context.ChatSessions
                .Where(s => s.UserId == userId.Value)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();

            return Ok(sessions.Select(ToSessionDto));
        }

        [HttpGet("sessions/{sessionId:int}/messages")]
        public async Task<IActionResult> GetMessages(int sessionId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });

            var session = await _context.ChatSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId.Value);
            if (session == null) return NotFound(new { message = "Không tìm thấy phiên trò chuyện." });

            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.ChatSessionId == sessionId)
                .OrderBy(m => m.Timestamp)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    Role = m.SenderRole == "User" ? "user" : "assistant",
                    Content = m.Content,
                    Timestamp = m.Timestamp
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost("sessions/{sessionId:int}/messages")]
        public async Task<IActionResult> SaveMessages(int sessionId, [FromBody] SaveChatMessagesRequest request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });

            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId.Value);
            if (session == null) return NotFound(new { message = "Không tìm thấy phiên trò chuyện." });

            if (!string.IsNullOrWhiteSpace(request.UserMessage))
            {
                _context.ChatMessages.Add(new ChatMessage
                {
                    ChatSessionId = sessionId,
                    SenderRole = "User",
                    Content = request.UserMessage.Trim()
                });
            }

            if (!string.IsNullOrWhiteSpace(request.AssistantMessage))
            {
                _context.ChatMessages.Add(new ChatMessage
                {
                    ChatSessionId = sessionId,
                    SenderRole = "Bot",
                    Content = request.AssistantMessage.Trim()
                });
            }

            if (string.IsNullOrWhiteSpace(session.Title) || session.Title == "Cuộc trò chuyện mới")
            {
                var title = request.UserMessage?.Trim() ?? "Cuộc trò chuyện";
                session.Title = title.Length > 50 ? title[..50] + "..." : title;
            }

            await _context.SaveChangesAsync();
            return Ok(new { saved = true });
        }

        private int? GetUserId()
        {
            var authorization = Request.Headers["Authorization"].ToString();
            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;

            var token = authorization["Bearer ".Length..];
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            try
            {
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var document = JsonDocument.Parse(json);
                return document.RootElement.TryGetProperty("sub", out var sub)
                    && int.TryParse(sub.GetString(), out var userId)
                    ? userId
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static ChatSessionDto ToSessionDto(ChatSession session)
        {
            return new ChatSessionDto
            {
                Id = session.Id,
                Title = session.Title,
                StartTime = session.StartTime
            };
        }
    }

    public class CreateChatSessionRequest
    {
        public string? Title { get; set; }
    }

    public class SaveChatMessagesRequest
    {
        public string? UserMessage { get; set; }
        public string? AssistantMessage { get; set; }
    }

    public class ChatSessionDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }

    public class ChatMessageDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = "assistant";
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
