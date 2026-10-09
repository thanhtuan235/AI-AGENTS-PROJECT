using eOfficeChatbot.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace eOfficeChatbot.Api.Data
{
    public static class DbSeeder
    {
        public static void SeedData(AppDbContext context)
        {
            // Tự động chạy Migration nếu chưa chạy (Rất tiện khi team làm việc chung)
            context.Database.Migrate();

            // Kiểm tra nếu đã có User thì bỏ qua để không sinh trùng lặp
            if (context.Users.Any()) return;

            // 1. Tạo phòng ban
            var deptIT = new Department { Name = "Phòng Công nghệ Thông tin" };
            var deptHR = new Department { Name = "Phòng Hành chính - Nhân sự" };
            context.Departments.AddRange(deptIT, deptHR);
            context.SaveChanges(); // Lưu để EF Core tự sinh Id

            // 2. Tạo Users (Role: Admin, Manager, Employee)
            var managerIT = new User { Username = "truongphong.it", FullName = "Nguyễn Văn Trưởng", Email = "manager.it@eoffice.vn", Role = "Manager", DepartmentId = deptIT.Id };
            var dev1 = new User { Username = "dev1", FullName = "Trần Hữu Nguyên Sơn", Email = "dev1@eoffice.vn", Role = "Employee", DepartmentId = deptIT.Id };
            var hr1 = new User { Username = "hr1", FullName = "Lê Thị C", Email = "hr1@eoffice.vn", Role = "Employee", DepartmentId = deptHR.Id };
            
            context.Users.AddRange(managerIT, dev1, hr1);
            context.SaveChanges();

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
    }
}