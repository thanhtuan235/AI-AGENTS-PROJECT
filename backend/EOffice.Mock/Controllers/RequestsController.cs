using System.Security.Claims;
using System.Text.Json;
using EOffice.Mock.Data;
using EOffice.Mock.DTOs;
using EOffice.Mock.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EOffice.Mock.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RequestsController : ControllerBase
    {
        private readonly EOfficeMockDbContext _context;

        public RequestsController(EOfficeMockDbContext context)
        {
            _context = context;
        }

        private (int UserId, string RoleCode, int DeptId) GetCurrentUserClaims()
        {
            var subVal = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleVal = User.FindFirstValue("role") ?? User.FindFirstValue(ClaimTypes.Role) ?? "Employee";
            var deptVal = User.FindFirstValue("dept_id");

            int.TryParse(subVal, out var userId);
            int.TryParse(deptVal, out var deptId);
            return (userId, roleVal, deptId);
        }

        // GET: api/requests?status=Pending&typeCode=Leave&limit=10
        [HttpGet]
        public async Task<IActionResult> GetMyRequests(
            [FromQuery] string? status,
            [FromQuery] string? typeCode,
            [FromQuery] int limit = 10)
        {
            var (userId, _, _) = GetCurrentUserClaims();
            if (userId <= 0)
                return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "Token không hợp lệ hoặc thiếu định danh người dùng."));

            var query = _context.Requests
                .AsNoTracking()
                .Include(r => r.RequestType)
                .Include(r => r.Approvals)
                    .ThenInclude(a => a.WorkflowStep)
                        .ThenInclude(ws => ws!.ApproverRole)
                .Where(r => r.UserID == userId);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.CurrentStatus == status);

            if (!string.IsNullOrWhiteSpace(typeCode))
                query = query.Where(r => r.RequestType!.TypeCode == typeCode);

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Take(Math.Clamp(limit, 1, 50))
                .ToListAsync();

            var result = items.Select(r =>
            {
                var currentStep = r.Approvals
                    .OrderBy(a => a.StepOrder)
                    .FirstOrDefault(a => a.Status == "Pending");

                return new
                {
                    r.RequestID,
                    r.Title,
                    RequestTypeCode = r.RequestType?.TypeCode,
                    RequestTypeName = r.RequestType?.TypeName,
                    r.CurrentStatus,
                    r.Payload,
                    r.CreatedAt,
                    r.UpdatedAt,
                    CurrentStep = currentStep == null ? null : new
                    {
                        currentStep.StepOrder,
                        StepName = currentStep.WorkflowStep?.StepName,
                        ApproverRole = currentStep.WorkflowStep?.ApproverRole?.RoleName,
                        currentStep.Status
                    }
                };
            });

            return Ok(ApiResponse<object>.Ok(result));
        }

        // GET: api/requests/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetRequestById(int id)
        {
            var (userId, roleCode, deptId) = GetCurrentUserClaims();

            var request = await _context.Requests
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.RequestType)
                .Include(r => r.FormTemplate)
                .Include(r => r.Approvals)
                    .ThenInclude(a => a.WorkflowStep)
                        .ThenInclude(ws => ws!.ApproverRole)
                .Include(r => r.Approvals)
                    .ThenInclude(a => a.ActedByUser)
                .Include(r => r.StatusHistories)
                    .ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(r => r.RequestID == id);

            if (request == null)
                return NotFound(ApiResponse<object>.Fail("NOT_FOUND", $"Không tìm thấy yêu cầu mã #{id}."));

            // Kiểm tra quyền truy cập: chủ đơn, Trưởng phòng cùng phòng ban, hoặc Admin
            bool isOwner = request.UserID == userId;
            bool isDeptManager = roleCode == "Manager" && request.User?.DepartmentID == deptId;
            bool isAdmin = roleCode == "Admin";

            if (!isOwner && !isDeptManager && !isAdmin)
                return StatusCode(403, ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền xem chi tiết yêu cầu này."));

            var data = new
            {
                request.RequestID,
                request.Title,
                RequestTypeCode = request.RequestType?.TypeCode,
                RequestTypeName = request.RequestType?.TypeName,
                TemplateTitle = request.FormTemplate?.Title,
                Requester = new
                {
                    request.UserID,
                    FullName = request.User?.FullName,
                    request.User?.DepartmentID
                },
                request.CurrentStatus,
                request.Payload,
                request.CreatedAt,
                request.UpdatedAt,
                Approvals = request.Approvals
                    .OrderBy(a => a.StepOrder)
                    .Select(a => new
                    {
                        a.ApprovalID,
                        a.StepOrder,
                        StepName = a.WorkflowStep?.StepName,
                        ApproverRole = a.WorkflowStep?.ApproverRole?.RoleName,
                        a.Status,
                        ActedBy = a.ActedByUser?.FullName,
                        a.ActedAt,
                        a.Comment
                    }),
                StatusHistories = request.StatusHistories
                    .OrderBy(h => h.ChangeNo)
                    .Select(h => new
                    {
                        h.ChangeNo,
                        h.OldStatus,
                        h.NewStatus,
                        h.ChangedAt,
                        ChangedBy = h.ChangedByUser?.FullName,
                        h.Note
                    })
            };

            return Ok(ApiResponse<object>.Ok(data));
        }

        // POST: api/requests
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto dto)
        {
            var (userId, _, _) = GetCurrentUserClaims();
            if (userId <= 0)
                return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "Token không hợp lệ."));

            // 1. Kiểm tra IdempotencyKey chống tạo trùng đơn
            var idempotencyKey = string.IsNullOrWhiteSpace(dto.IdempotencyKey)
                ? Guid.NewGuid().ToString()
                : dto.IdempotencyKey.Trim();

            var existingRequest = await _context.Requests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey);

            if (existingRequest != null)
            {
                return Ok(ApiResponse<object>.Ok(new
                {
                    existingRequest.RequestID,
                    existingRequest.Title,
                    existingRequest.CurrentStatus,
                    Message = "Yêu cầu đã được ghi nhận trước đó (Idempotent)."
                }));
            }

            // 2. Tìm RequestType & FormTemplate tương ứng
            var requestType = await _context.RequestTypes
                .Include(rt => rt.WorkflowSteps)
                .FirstOrDefaultAsync(rt => rt.TypeCode == dto.RequestTypeCode && rt.IsActive);

            if (requestType == null)
                return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST_TYPE", $"Loại yêu cầu '{dto.RequestTypeCode}' không tồn tại hoặc đã ngừng hỗ trợ."));

            var template = dto.TemplateID.HasValue
                ? await _context.FormTemplates.FirstOrDefaultAsync(ft => ft.TemplateID == dto.TemplateID.Value && ft.RequestTypeID == requestType.RequestTypeID && ft.IsActive)
                : await _context.FormTemplates.Where(ft => ft.RequestTypeID == requestType.RequestTypeID && ft.IsActive).OrderByDescending(ft => ft.TemplateID).FirstOrDefaultAsync();

            if (template == null)
                return BadRequest(ApiResponse<object>.Fail("TEMPLATE_NOT_FOUND", "Không tìm thấy biểu mẫu khả dụng cho loại yêu cầu này."));

            // 3. Chuẩn hóa Payload JSON & kiểm tra các trường bắt buộc từ FieldsJson
            string rawPayload;
            Dictionary<string, JsonElement>? payloadDict;
            try
            {
                if (dto.Payload.ValueKind == JsonValueKind.String)
                {
                    rawPayload = dto.Payload.GetString() ?? "{}";
                    payloadDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rawPayload);
                }
                else if (dto.Payload.ValueKind == JsonValueKind.Object)
                {
                    rawPayload = dto.Payload.GetRawText();
                    payloadDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rawPayload);
                }
                else
                {
                    return BadRequest(ApiResponse<object>.Fail("INVALID_PAYLOAD", "Payload phải là JSON object hoặc JSON string hợp lệ."));
                }
            }
            catch
            {
                return BadRequest(ApiResponse<object>.Fail("INVALID_PAYLOAD", "Không thể phân tích cú pháp JSON của Payload."));
            }

            var requiredFields = JsonSerializer.Deserialize<List<string>>(template.FieldsJson) ?? new List<string>();
            var missingFields = requiredFields
                .Where(field => payloadDict == null
                    || !payloadDict.TryGetValue(field, out var val)
                    || val.ValueKind == JsonValueKind.Null
                    || (val.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(val.GetString())))
                .ToList();

            if (missingFields.Count > 0)
            {
                return BadRequest(ApiResponse<object>.Fail(
                    "MISSING_REQUIRED_FIELDS",
                    $"Thiếu thông tin bắt buộc theo biểu mẫu '{template.Title}': {string.Join(", ", missingFields)}."));
            }

            // 4. Khởi tạo Request + Workflow Approvals + Initial Status History
            var now = DateTime.UtcNow;
            var newRequest = new Request
            {
                Title = string.IsNullOrWhiteSpace(dto.Title) ? $"{requestType.TypeName} - {now:dd/MM/yyyy}" : dto.Title.Trim(),
                Payload = rawPayload,
                CurrentStatus = "Pending",
                CreatedAt = now,
                UpdatedAt = now,
                IdempotencyKey = idempotencyKey,
                UserID = userId,
                RequestTypeID = requestType.RequestTypeID,
                TemplateID = template.TemplateID
            };

            foreach (var step in requestType.WorkflowSteps.OrderBy(s => s.StepOrder))
            {
                newRequest.Approvals.Add(new RequestApproval
                {
                    StepOrder = step.StepOrder,
                    WorkflowStepID = step.WorkflowStepID,
                    Status = "Pending"
                });
            }

            newRequest.StatusHistories.Add(new RequestStatusHistory
            {
                ChangeNo = 1,
                OldStatus = null,
                NewStatus = "Pending",
                ChangedAt = now,
                ChangedByUserID = userId,
                Note = "Khởi tạo yêu cầu hành chính"
            });

            _context.Requests.Add(newRequest);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                newRequest.RequestID,
                newRequest.Title,
                RequestTypeCode = requestType.TypeCode,
                newRequest.CurrentStatus,
                TotalApprovalSteps = newRequest.Approvals.Count,
                newRequest.CreatedAt
            }));
        }

        // PUT: api/requests/{id}/cancel
        [HttpPut("{id:int}/cancel")]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var (userId, _, _) = GetCurrentUserClaims();

            var request = await _context.Requests
                .Include(r => r.Approvals)
                .Include(r => r.StatusHistories)
                .FirstOrDefaultAsync(r => r.RequestID == id);

            if (request == null)
                return NotFound(ApiResponse<object>.Fail("NOT_FOUND", $"Không tìm thấy yêu cầu mã #{id}."));

            if (request.UserID != userId)
                return StatusCode(403, ApiResponse<object>.Fail("FORBIDDEN", "Bạn chỉ có thể hủy yêu cầu do chính mình tạo."));

            if (request.CurrentStatus != "Pending")
                return BadRequest(ApiResponse<object>.Fail("INVALID_STATUS", $"Không thể hủy đơn đang ở trạng thái '{request.CurrentStatus}'. Chỉ đơn 'Pending' mới được phép hủy."));

            var now = DateTime.UtcNow;
            var oldStatus = request.CurrentStatus;
            request.CurrentStatus = "Cancelled";
            request.UpdatedAt = now;

            foreach (var approval in request.Approvals.Where(a => a.Status == "Pending"))
            {
                approval.Status = "Skipped";
            }

            var nextChangeNo = (request.StatusHistories.Max(h => (int?)h.ChangeNo) ?? 0) + 1;
            request.StatusHistories.Add(new RequestStatusHistory
            {
                ChangeNo = nextChangeNo,
                OldStatus = oldStatus,
                NewStatus = "Cancelled",
                ChangedAt = now,
                ChangedByUserID = userId,
                Note = "Người tạo yêu cầu hủy đơn"
            });

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                request.RequestID,
                request.CurrentStatus,
                request.UpdatedAt
            }));
        }

        // PUT: api/requests/{id}/approve (Phục vụ Manager/Admin duyệt hoặc từ chối bước hiện tại)
        [HttpPut("{id:int}/approve")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> ProcessApprovalStep(int id, [FromBody] ApproveStepDto dto)
        {
            var (userId, roleCode, deptId) = GetCurrentUserClaims();

            var request = await _context.Requests
                .Include(r => r.User)
                .Include(r => r.Approvals)
                .Include(r => r.StatusHistories)
                .FirstOrDefaultAsync(r => r.RequestID == id);

            if (request == null)
                return NotFound(ApiResponse<object>.Fail("NOT_FOUND", $"Không tìm thấy yêu cầu mã #{id}."));

            if (roleCode == "Manager" && request.User?.DepartmentID != deptId)
                return StatusCode(403, ApiResponse<object>.Fail("FORBIDDEN", "Trưởng phòng chỉ được phê duyệt yêu cầu của nhân viên thuộc phòng mình."));

            if (request.CurrentStatus != "Pending")
                return BadRequest(ApiResponse<object>.Fail("INVALID_STATUS", $"Yêu cầu đã kết thúc với trạng thái '{request.CurrentStatus}'."));

            var currentStep = request.Approvals
                .OrderBy(a => a.StepOrder)
                .FirstOrDefault(a => a.Status == "Pending");

            if (currentStep == null)
                return BadRequest(ApiResponse<object>.Fail("NO_PENDING_STEP", "Không còn bước phê duyệt nào đang chờ xử lý."));

            var now = DateTime.UtcNow;
            currentStep.Status = dto.IsApproved ? "Approved" : "Rejected";
            currentStep.ActedAt = now;
            currentStep.ActedByUserID = userId;
            currentStep.Comment = dto.Comment;

            var oldStatus = request.CurrentStatus;
            bool hasMorePending = request.Approvals.Any(a => a.Status == "Pending");

            if (!dto.IsApproved)
            {
                request.CurrentStatus = "Rejected";
            }
            else if (!hasMorePending)
            {
                request.CurrentStatus = "Approved";
            }

            request.UpdatedAt = now;

            if (request.CurrentStatus != oldStatus)
            {
                var nextChangeNo = (request.StatusHistories.Max(h => (int?)h.ChangeNo) ?? 0) + 1;
                request.StatusHistories.Add(new RequestStatusHistory
                {
                    ChangeNo = nextChangeNo,
                    OldStatus = oldStatus,
                    NewStatus = request.CurrentStatus,
                    ChangedAt = now,
                    ChangedByUserID = userId,
                    Note = dto.Comment ?? (dto.IsApproved ? "Đã phê duyệt hoàn tất" : "Từ chối yêu cầu")
                });
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                request.RequestID,
                request.CurrentStatus,
                ProcessedStep = currentStep.StepOrder,
                StepStatus = currentStep.Status
            }));
        }
    }
}