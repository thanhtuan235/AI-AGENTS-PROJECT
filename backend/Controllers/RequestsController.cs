using System.Text.Json;
using eOfficeChatbot.Api.Data;
using eOfficeChatbot.Api.DTOs;
using eOfficeChatbot.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eOfficeChatbot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RequestsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/requests/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequestStatus(int id)
        {
            var request = await _context.Requests
                .Include(r => r.User)
                .Where(r => r.Id == id)
                .Select(r => new 
                {
                    r.Id,
                    r.RequestType,
                    r.Title,
                    r.Status,
                    r.CreatedAt,
                    RequesterName = r.User.FullName,
                    Payload = r.PayloadDetail
                })
                .FirstOrDefaultAsync();

            if (request == null) return NotFound(new { Message = "Không tìm thấy đơn yêu cầu." });

            return Ok(request);
        }

        // POST: api/requests/leave
        [HttpPost("leave")]
        public async Task<IActionResult> CreateLeaveRequest([FromBody] LeaveRequestDto dto)
        {
            // Kiểm tra xem User có tồn tại không
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return BadRequest(new { Message = "User không tồn tại." });

            // Đóng gói chi tiết thành JSON string
            var payloadStr = JsonSerializer.Serialize(new { 
                dto.StartDate, 
                dto.EndDate, 
                dto.Reason 
            });

            var newRequest = new Request
            {
                UserId = dto.UserId,
                RequestType = "Leave",
                Title = $"Xin nghỉ phép: {user.FullName}",
                PayloadDetail = payloadStr,
                Status = "Pending"
            };

            _context.Requests.Add(newRequest);
            await _context.SaveChangesAsync(); // Lưu để EF sinh ra ID cho Request

            // Ghi lại lịch sử (Audit Log)
            var history = new RequestStatusHistory
            {
                RequestId = newRequest.Id,
                ChangedByUserId = dto.UserId, // Giả sử user tự tạo
                OldStatus = "Draft",
                NewStatus = "Pending",
                Note = "Hệ thống AI tạo tự động từ chat"
            };
            
            _context.RequestStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            // Trả về JSON xác nhận cho Agent
            return Ok(new { 
                Message = "Tạo đơn thành công!", 
                RequestId = newRequest.Id, 
                Status = newRequest.Status 
            });
        }
        // PUT: api/requests/{id}/cancel
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request == null) return NotFound(new { Message = "Không tìm thấy đơn." });

            if (request.Status != "Pending") 
                return BadRequest(new { Message = "Chỉ có thể hủy đơn đang ở trạng thái Pending." });

            request.Status = "Cancelled";
            
            // Ghi log vào lịch sử
            _context.RequestStatusHistories.Add(new RequestStatusHistory
            {
                RequestId = request.Id,
                ChangedByUserId = request.UserId,
                OldStatus = "Pending",
                NewStatus = "Cancelled",
                Note = "User yêu cầu hủy thông qua Chatbot"
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Hủy đơn thành công", RequestId = request.Id, Status = request.Status });
        }
    }
}