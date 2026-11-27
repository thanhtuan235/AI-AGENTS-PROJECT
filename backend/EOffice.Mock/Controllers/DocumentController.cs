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
    public class DocumentsController : ControllerBase
    {
        private readonly EOfficeMockDbContext _context;

        public DocumentsController(EOfficeMockDbContext context)
        {
            _context = context;
        }

        private string GetCurrentRoleCode()
        {
            return User.FindFirstValue("role") ?? User.FindFirstValue(ClaimTypes.Role) ?? "Employee";
        }

        // GET: api/documents?category=NhanSu&keyword=nghi phep
        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] string? category, [FromQuery] string? keyword)
        {
            var roleCode = GetCurrentRoleCode();

            var query = _context.Documents
                .AsNoTracking()
                .Include(d => d.Category)
                .Include(d => d.IssuedByDepartment)
                .Where(d => d.Status == "Active")
                .Where(d => d.AccessLevel == "Public"
                    || d.DocumentRoleAccesses.Any(dra => dra.Role!.RoleCode == roleCode));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(d => d.Category!.CategoryName == category);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(d => d.Title.Contains(keyword) || (d.Summary != null && d.Summary.Contains(keyword)));

            var docs = await query
                .OrderByDescending(d => d.EffectiveDate)
                .Select(d => new
                {
                    d.DocumentID,
                    d.Title,
                    Category = d.Category!.CategoryName,
                    IssuedByDepartment = d.IssuedByDepartment!.DepartmentName,
                    d.Summary,
                    d.FileUrl,
                    d.Version,
                    d.AccessLevel,
                    d.EffectiveDate,
                    d.IndexStatus
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(docs));
        }

        // GET: api/documents/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDocumentById(int id)
        {
            var roleCode = GetCurrentRoleCode();

            var doc = await _context.Documents
                .AsNoTracking()
                .Include(d => d.Category)
                .Include(d => d.IssuedByDepartment)
                .Include(d => d.DocumentRoleAccesses)
                    .ThenInclude(dra => dra.Role)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (doc == null)
                return NotFound(ApiResponse<object>.Fail("NOT_FOUND", $"Không tìm thấy tài liệu mã #{id}."));

            bool hasAccess = doc.AccessLevel == "Public"
                || doc.DocumentRoleAccesses.Any(dra => dra.Role?.RoleCode == roleCode);

            if (!hasAccess)
                return StatusCode(403, ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền truy cập tài liệu này."));

            return Ok(ApiResponse<object>.Ok(new
            {
                doc.DocumentID,
                doc.Title,
                Category = doc.Category?.CategoryName,
                IssuedByDepartment = doc.IssuedByDepartment?.DepartmentName,
                doc.Summary,
                doc.FileUrl,
                doc.Version,
                doc.AccessLevel,
                doc.Status,
                doc.IndexStatus,
                doc.EffectiveDate,
                doc.ExpiryDate
            }));
        }

        // GET: api/documents/authorized-ids (AI Service dùng để filter Vector DB theo RBAC)
        [HttpGet("authorized-ids")]
        public async Task<IActionResult> GetAuthorizedDocumentIds()
        {
            var roleCode = GetCurrentRoleCode();

            var authorizedDocs = await _context.Documents
                .AsNoTracking()
                .Where(d => d.Status == "Active")
                .Where(d => d.AccessLevel == "Public"
                    || d.DocumentRoleAccesses.Any(dra => dra.Role!.RoleCode == roleCode))
                .Select(d => new
                {
                    d.DocumentID,
                    Category = d.Category!.CategoryName
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                RoleCode = roleCode,
                DocumentIds = authorizedDocs.Select(d => d.DocumentID).ToList(),
                Categories = authorizedDocs.Select(d => d.Category).Distinct().ToList()
            }));
        }

        // GET: api/documents/forms?category=Leave (Tool list_forms của AI Agent)
        [HttpGet("forms")]
        public async Task<IActionResult> ListForms([FromQuery] string? category)
        {
            var query = _context.FormTemplates
                .AsNoTracking()
                .Include(ft => ft.RequestType)
                .Where(ft => ft.IsActive && ft.RequestType!.IsActive);

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(ft => ft.RequestType!.TypeCode == category);
            }

            var forms = await query
                .Select(ft => new
                {
                    ft.TemplateID,
                    Category = ft.RequestType!.TypeCode, // Giữ tên Category để khớp contract tool list_forms
                    RequestTypeName = ft.RequestType.TypeName,
                    ft.Title,
                    ft.Version,
                    ft.FieldsJson,
                    ft.DownloadUrl,
                    ft.Instructions
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(forms));
        }

        // POST: api/documents/forms (Admin tạo biểu mẫu mới & cấu hình FieldsJson)
        [HttpPost("forms")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateFormTemplate([FromBody] CreateFormTemplateDto dto)
        {
            var requestType = await _context.RequestTypes
                .FirstOrDefaultAsync(rt => rt.TypeCode == dto.RequestTypeCode && rt.IsActive);

            if (requestType == null)
                return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST_TYPE", $"Không tìm thấy loại yêu cầu '{dto.RequestTypeCode}'."));

            var form = new FormTemplate
            {
                RequestTypeID = requestType.RequestTypeID,
                Title = dto.Title.Trim(),
                Version = string.IsNullOrWhiteSpace(dto.Version) ? "1.0" : dto.Version.Trim(),
                FieldsJson = JsonSerializer.Serialize(dto.RequiredFields),
                DownloadUrl = dto.DownloadUrl,
                Instructions = dto.Instructions,
                IsActive = true
            };

            _context.FormTemplates.Add(form);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                form.TemplateID,
                Category = requestType.TypeCode,
                form.Title,
                form.Version,
                form.FieldsJson,
                form.DownloadUrl
            }));
        }
    }
}