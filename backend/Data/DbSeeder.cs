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

            // 3. Tạo Documents & Form Templates (Để AI Agent có cái tra cứu)
            context.Documents.Add(new Document 
            { 
                Title = "Quy định nghỉ phép 2026", 
                Category = "NhanSu", 
                Summary = "Nhân viên chính thức có 12 ngày phép/năm. Nghỉ quá 3 ngày cần báo trước 1 tuần.", 
                ContentUrl = "/docs/quy-dinh-nghi-phep.pdf" 
            });
            context.FormTemplates.Add(new FormTemplate 
            { 
                Title = "Mẫu Đơn Xin Nghỉ Phép", 
                Category = "Leave", 
                DownloadUrl = "/forms/don-xin-nghi-phep.docx" 
            });
            context.SaveChanges();

            // 4. Tạo các Request (Dùng JSON để linh hoạt lưu chi tiết)
            var leaveRequest = new Request 
            { 
                UserId = dev1.Id, 
                RequestType = "Leave", 
                Title = "Xin nghỉ phép đi khám bệnh", 
                PayloadDetail = "{\"StartDate\":\"2026-10-05\", \"EndDate\":\"2026-10-05\", \"Reason\":\"Đi khám sức khỏe định kỳ\"}", 
                Status = "Pending" 
            };
            context.Requests.Add(leaveRequest);
            context.SaveChanges();

            // 5. Ghi Log lịch sử Request
            context.RequestStatusHistories.Add(new RequestStatusHistory 
            { 
                RequestId = leaveRequest.Id, 
                ChangedByUserId = dev1.Id, 
                OldStatus = "Draft", 
                NewStatus = "Pending", 
                Note = "Gửi đơn cho sếp duyệt" 
            });
            context.SaveChanges();
        }
    }
}