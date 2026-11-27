using eOfficeChatbot.Api.Data;
using eOfficeChatbot.Api.DTOs;
using eOfficeChatbot.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace eOfficeChatbot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FeedbackController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackDto dto)
        {
            var feedback = new Feedback
            {
                ChatSessionId = dto.ChatSessionId,
                MessageId = dto.MessageId,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đã ghi nhận phản hồi", FeedbackId = feedback.Id });
        }
    }
}