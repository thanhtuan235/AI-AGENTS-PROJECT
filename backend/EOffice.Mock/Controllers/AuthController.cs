using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EOffice.Mock.Data;
using EOffice.Mock.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EOffice.Mock.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly EOfficeMockDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(EOfficeMockDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public record LoginRequest(string Username, string Password);

        // POST: /auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest(ApiResponse<object>.Fail(
                    "INVALID_INPUT",
                    "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu."));
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == req.Username.Trim());

            if (user == null || string.IsNullOrEmpty(user.PasswordHash) ||
                !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "UNAUTHORIZED",
                    "Sai tên đăng nhập hoặc mật khẩu."));
            }

            if (!user.IsActive)
            {
                return StatusCode(403, ApiResponse<object>.Fail(
                    "ACCOUNT_DISABLED",
                    "Tài khoản của bạn đã bị vô hiệu hóa."));
            }

            var roleCode = user.Role?.RoleCode ?? "Employee";
            var expireMinutes = int.TryParse(_config["Jwt:ExpireMinutes"], out var mins) ? mins : 60;
            var jwtKey = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("Chưa cấu hình Jwt:Key trong appsettings.json.");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Claims chuẩn theo hợp đồng JWT Phase 0
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
                new Claim("name", user.FullName),
                new Claim("role", roleCode),
                new Claim("dept_id", user.DepartmentID.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(ApiResponse<object>.Ok(new
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = expireMinutes * 60,
                user = new
                {
                    user_id = user.UserID,
                    username = user.Username,
                    name = user.FullName,
                    email = user.Email,
                    role = roleCode,
                    dept_id = user.DepartmentID,
                    dept_name = user.Department?.DepartmentName
                }
            }));
        }

        // GET: /auth/me (FE và AI Service dùng để kiểm tra token & lấy profile hiện tại)
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var subClaim = User.FindFirstValue("sub");
            if (!int.TryParse(subClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "INVALID_TOKEN",
                    "Token không chứa định danh người dùng hợp lệ."));
            }

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null || !user.IsActive)
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "USER_NOT_FOUND",
                    "Người dùng không tồn tại hoặc đã bị khóa."));
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                user_id = user.UserID,
                username = user.Username,
                name = user.FullName,
                email = user.Email,
                role = user.Role?.RoleCode ?? "Employee",
                role_name = user.Role?.RoleName,
                dept_id = user.DepartmentID,
                dept_code = user.Department?.DepartmentCode,
                dept_name = user.Department?.DepartmentName
            }));
        }
    }
}