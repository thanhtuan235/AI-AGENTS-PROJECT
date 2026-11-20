using eOfficeChatbot.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace eOfficeChatbot.Api.Data
{
    public static class DbSeeder
    {
        public static void SeedData(AppDbContext context)
        {
            // Tự động chạy Migration nếu chưa chạy (Rất tiện khi team làm việc chung)
            context.Database.Migrate();

            // 1. Tạo phòng ban
            var deptIT = context.Departments.FirstOrDefault(d => d.Name == "Phòng Công nghệ Thông tin")
                ?? new Department { Name = "Phòng Công nghệ Thông tin" };
            var deptHR = context.Departments.FirstOrDefault(d => d.Name == "Phòng Hành chính - Nhân sự")
                ?? new Department { Name = "Phòng Hành chính - Nhân sự" };
            if (deptIT.Id == 0) context.Departments.Add(deptIT);
            if (deptHR.Id == 0) context.Departments.Add(deptHR);
            context.SaveChanges();

            // 2. Đảm bảo luôn có đúng 3 tài khoản mẫu
            var managerIT = EnsureUser(context, "truongphong.it", "Nguyễn Văn Trưởng", "manager.it@eoffice.vn", "Manager", deptIT.Id);
            var dev1 = EnsureUser(context, "dev1", "Trần Hữu Nguyên Sơn", "dev1@eoffice.vn", "Employee", deptIT.Id);
            var hr1 = EnsureUser(context, "hr1", "Lê Thị C", "hr1@eoffice.vn", "Admin", deptHR.Id);
            context.SaveChanges();

            if (context.Documents.Any()) return;

            // Gán Manager cho phòng IT
            deptIT.ManagerId = managerIT.Id;
            context.SaveChanges();

            /// 3. Tạo Documents & Form Templates (Chuẩn hóa tên Category theo tài liệu)
            context.Documents.Add(new Document 
            { 
                Title = "Quy định nghỉ phép 2026", 
                Category = "NhanSu", // Viết liền, không dấu
                Summary = "Nhân viên chính thức có 12 ngày phép/năm. Nghỉ quá 3 ngày cần báo trước 1 tuần.", 
                ContentUrl = "/docs/quy-dinh-nghi-phep.pdf" 
            });
            context.FormTemplates.Add(new FormTemplate 
            { 
                Title = "Mẫu Đơn Xin Nghỉ Phép", 
                Category = "Leave", // Phải khớp với RequestType
                DownloadUrl = "/forms/don-xin-nghi-phep.docx",
                FieldsJson = "[\"StartDate\", \"EndDate\", \"Reason\"]" // Agent sẽ dùng mảng này để hỏi user
            });
            context.SaveChanges();

            // 4. Tạo các Request (Thử nghiệm trạng thái Cancelled)
            var leaveRequest = new Request 
            { 
                UserId = dev1.Id, 
                RequestType = "Leave", 
                Title = "Xin nghỉ phép đi khám bệnh", 
                PayloadDetail = "{\"StartDate\":\"2026-10-05\", \"EndDate\":\"2026-10-05\", \"Reason\":\"Đi khám sức khỏe định kỳ\"}", 
                Status = "Pending" 
            };
            var cancelledRequest = new Request 
            { 
                UserId = dev1.Id, 
                RequestType = "Leave", 
                Title = "Xin nghỉ phép đi du lịch", 
                PayloadDetail = "{\"StartDate\":\"2026-11-05\", \"EndDate\":\"2026-11-06\", \"Reason\":\"Việc cá nhân\"}", 
                Status = "Cancelled" // Trạng thái hủy
            };
            context.Requests.AddRange(leaveRequest, cancelledRequest);
            context.SaveChanges();

            // 5. Khởi tạo Workflow Steps cho đơn Pending
            context.WorkflowSteps.Add(new WorkflowStep
            {
                RequestId = leaveRequest.Id,
                StepName = "Quản lý trực tiếp duyệt",
                ApproverRole = "Manager",
                Status = "Pending"
            });
            context.SaveChanges();

            
        }

        private static User EnsureUser(AppDbContext context, string username, string fullName, string email, string role, int departmentId)
        {
            var user = context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                user = new User { Username = username };
                context.Users.Add(user);
            }

            user.FullName = fullName;
            user.Email = email;
            user.Role = role;
            user.DepartmentId = departmentId;
            user.PasswordHash = HashPassword("123456");
            return user;
        }

        public static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}