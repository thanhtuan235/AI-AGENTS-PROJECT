using System.Text.Json;

namespace EOffice.Mock.DTOs
{
    public record ApiError(string Code, string Message);

    public record ApiResponse<T>(bool Success, T? Data, ApiError? Error)
    {
        public static ApiResponse<T> Ok(T data) => new(true, data, null);
        public static ApiResponse<T> Fail(string code, string message) => new(false, default, new ApiError(code, message));
    }

    public class CreateRequestDto
    {
        public string RequestTypeCode { get; set; } = string.Empty; // Leave, Purchase, RoomBooking
        public int? TemplateID { get; set; } // Tùy chọn: nếu null sẽ tự lấy mẫu Active mới nhất
        public string Title { get; set; } = string.Empty;
        public JsonElement Payload { get; set; } // Hỗ trợ cả JSON Object lẫn JSON String
        public string? IdempotencyKey { get; set; }
    }

    public class ApproveStepDto
    {
        public bool IsApproved { get; set; }
        public string? Comment { get; set; }
    }

    public class CreateFormTemplateDto
    {
        public string RequestTypeCode { get; set; } = string.Empty; // Leave, Purchase, RoomBooking
        public string Title { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public List<string> RequiredFields { get; set; } = new();
        public string? DownloadUrl { get; set; }
        public string? Instructions { get; set; }
    }
}