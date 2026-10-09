using eOfficeChatbot.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eOfficeChatbot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DocumentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/documents/search?keyword=nghỉ phép
        [HttpGet("search")]
        public async Task<IActionResult> SearchDocuments([FromQuery] string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return BadRequest("Keyword is required.");

            var docs = await _context.Documents
                .Where(d => d.Title.Contains(keyword) || d.Summary.Contains(keyword))
                .Select(d => new { d.Id, d.Title, d.Category, d.Summary, d.ContentUrl }) // Chỉ lấy trường cần thiết
                .ToListAsync();

            return Ok(docs);
        }

        // GET: api/documents/forms?category=Leave
        [HttpGet("forms")]
        public async Task<IActionResult> ListForms([FromQuery] string? category)
        {
            var query = _context.FormTemplates.AsQueryable();
            
            if (!string.IsNullOrEmpty(category))
                query = query.Where(f => f.Category == category);

            var forms = await query
                .Select(f => new { f.Id, f.Title, f.Category, f.DownloadUrl, f.FieldsJson })
                .ToListAsync();

            return Ok(forms);
        }
    }
}