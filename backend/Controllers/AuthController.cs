using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eOfficeChatbot.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eOfficeChatbot.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Vui lòng nhập tên đăng nhập và mật khẩu." });
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || user.PasswordHash != DbSeeder.HashPassword(request.Password))
            {
                return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng." });
            }

            var exp = DateTimeOffset.UtcNow.AddHours(8).ToUnixTimeSeconds();
            var payload = new Dictionary<string, object>
            {
                ["sub"] = user.Id.ToString(),
                ["name"] = user.FullName,
                ["role"] = user.Role,
                ["dept_id"] = user.DepartmentId,
                ["exp"] = exp
            };

            var header = Base64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
            var body = Base64UrlEncode(JsonSerializer.Serialize(payload));
            var unsigned = $"{header}.{body}";
            var secret = Encoding.UTF8.GetBytes("eOfficeChatbot-Development-Secret-Key-2026");
            using var hmac = new HMACSHA256(secret);
            var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.ASCII.GetBytes(unsigned)));

            return Ok(new { token = $"{unsigned}.{signature}" });
        }

        private static string Base64UrlEncode(string value)
        {
            return Base64UrlEncode(Encoding.UTF8.GetBytes(value));
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
