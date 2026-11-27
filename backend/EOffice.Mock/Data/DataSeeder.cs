using EOffice.Mock.Models;
using Microsoft.EntityFrameworkCore;

namespace EOffice.Mock.Data
{
    public static class DbSeeder
    {
        public static void Seed(EOfficeMockDbContext context)
        {
            context.Database.Migrate();

            if (context.Roles.Any()) return;

            using var transaction = context.Database.BeginTransaction();
            try
            {
                var now = DateTime.UtcNow;
                var devPasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");

                // 1. Roles
                var roleEmployee = new Role { RoleCode = "Employee", RoleName = "Nhân viên", Description = "Nhân viên nghiệp vụ thông thường" };
                var roleManager = new Role { RoleCode = "Manager", RoleName = "Trưởng phòng", Description = "Quản lý phòng ban, có quyền phê duyệt và xem thống kê phòng" };
                var roleAdmin = new Role { RoleCode = "Admin", RoleName = "Quản trị viên", Description = "Quản trị hệ thống, kho tri thức và cấu hình" };
                context.Roles.AddRange(roleEmployee, roleManager, roleAdmin);
                context.SaveChanges();

                // 2. Permissions & RolePermissions
                var permCreateReq = new Permission { PermissionCode = "REQUEST_CREATE", Description = "Tạo yêu cầu hành chính" };
                var permApproveReq = new Permission { PermissionCode = "REQUEST_APPROVE", Description = "Phê duyệt yêu cầu hành chính" };
                var permViewDeptStats = new Permission { PermissionCode = "STATS_VIEW_DEPT", Description = "Xem thống kê dữ liệu phòng ban" };
                var permManageDocs = new Permission { PermissionCode = "DOC_MANAGE", Description = "Quản lý tài liệu và biểu mẫu" };
                context.Permissions.AddRange(permCreateReq, permApproveReq, permViewDeptStats, permManageDocs);
                context.SaveChanges();

                context.RolePermissions.AddRange(
                    new RolePermission { RoleID = roleEmployee.RoleID, PermissionID = permCreateReq.PermissionID },
                    new RolePermission { RoleID = roleManager.RoleID, PermissionID = permCreateReq.PermissionID },
                    new RolePermission { RoleID = roleManager.RoleID, PermissionID = permApproveReq.PermissionID },
                    new RolePermission { RoleID = roleManager.RoleID, PermissionID = permViewDeptStats.PermissionID },
                    new RolePermission { RoleID = roleAdmin.RoleID, PermissionID = permCreateReq.PermissionID },
                    new RolePermission { RoleID = roleAdmin.RoleID, PermissionID = permApproveReq.PermissionID },
                    new RolePermission { RoleID = roleAdmin.RoleID, PermissionID = permViewDeptStats.PermissionID },
                    new RolePermission { RoleID = roleAdmin.RoleID, PermissionID = permManageDocs.PermissionID }
                );
                context.SaveChanges();

                // 3. Departments (HeadUserID để null trước để tránh vòng lặp FK với User)
                var deptIT = new Department { DepartmentCode = "IT", DepartmentName = "Phòng Công nghệ Thông tin", HeadUserID = null };
                var deptHR = new Department { DepartmentCode = "HR", DepartmentName = "Phòng Nhân sự - Hành chính", HeadUserID = null };
                context.Departments.AddRange(deptIT, deptHR);
                context.SaveChanges();

                // 4. Users (mật khẩu chung: 123456)
                var adminUser = new User
                {
                    Username = "admin",
                    FirstName = "Quản",
                    LastName = "Trần Minh",
                    Email = "admin@bank.local",
                    PhoneNumber = "0901000001",
                    Gender = "Male",
                    PasswordHash = devPasswordHash,
                    IsActive = true,
                    LastSyncedAt = now,
                    DepartmentID = deptIT.DepartmentID,
                    RoleID = roleAdmin.RoleID
                };
                var managerIT = new User
                {
                    Username = "truongphong.it",
                    FirstName = "Trưởng",
                    LastName = "Nguyễn Văn",
                    Email = "truongphong.it@bank.local",
                    PhoneNumber = "0901000002",
                    Gender = "Male",
                    PasswordHash = devPasswordHash,
                    IsActive = true,
                    LastSyncedAt = now,
                    DepartmentID = deptIT.DepartmentID,
                    RoleID = roleManager.RoleID
                };
                var dev1 = new User
                {
                    Username = "dev1",
                    FirstName = "An",
                    LastName = "Lê Hoàng",
                    Email = "dev1@bank.local",
                    PhoneNumber = "0901000003",
                    Gender = "Male",
                    PasswordHash = devPasswordHash,
                    IsActive = true,
                    LastSyncedAt = now,
                    DepartmentID = deptIT.DepartmentID,
                    RoleID = roleEmployee.RoleID
                };
                var managerHR = new User
                {
                    Username = "truongphong.hr",
                    FirstName = "Lan",
                    LastName = "Phạm Thị",
                    Email = "truongphong.hr@bank.local",
                    PhoneNumber = "0901000004",
                    Gender = "Female",
                    PasswordHash = devPasswordHash,
                    IsActive = true,
                    LastSyncedAt = now,
                    DepartmentID = deptHR.DepartmentID,
                    RoleID = roleManager.RoleID
                };
                var hr1 = new User
                {
                    Username = "hr1",
                    FirstName = "Mai",
                    LastName = "Đỗ Ngọc",
                    Email = "hr1@bank.local",
                    PhoneNumber = "0901000005",
                    Gender = "Female",
                    PasswordHash = devPasswordHash,
                    IsActive = true,
                    LastSyncedAt = now,
                    DepartmentID = deptHR.DepartmentID,
                    RoleID = roleEmployee.RoleID
                };
                context.Users.AddRange(adminUser, managerIT, dev1, managerHR, hr1);
                context.SaveChanges();

                // 5. Cập nhật ngược HeadUserID cho Departments
                deptIT.HeadUserID = managerIT.UserID;
                deptHR.HeadUserID = managerHR.UserID;
                context.SaveChanges();

                // 6. Categories
                var catNhanSu = new Category { CategoryName = "NhanSu", Description = "Quy định, chế độ, nghỉ phép và phúc lợi nhân sự" };
                var catHanhChinh = new Category { CategoryName = "HanhChinh", Description = "Quy trình mua sắm, công tác phí và vận hành văn phòng" };
                context.Categories.AddRange(catNhanSu, catHanhChinh);
                context.SaveChanges();

                // 7. Documents & DocumentRoleAccess
                var docLeavePolicy = new Document
                {
                    Title = "Quy định nghỉ phép và chế độ phúc lợi 2026",
                    Summary = "Nhân viên chính thức có 12 ngày phép năm. Nghỉ từ 3 ngày trở lên cần báo trước tối thiểu 5 ngày làm việc.",
                    FileUrl = "/docs/quy-dinh-nghi-phep-2026.pdf",
                    Version = "1.0",
                    EffectiveDate = new DateTime(2026, 1, 1),
                    AccessLevel = "Internal",
                    Status = "Active",
                    IndexStatus = "Indexed",
                    CreatedAt = now,
                    CategoryID = catNhanSu.CategoryID,
                    IssuedByDepartmentID = deptHR.DepartmentID,
                    UploadedByUserID = adminUser.UserID
                };
                var docPurchasePolicy = new Document
                {
                    Title = "Quy chế hạn mức phê duyệt mua sắm thiết bị CNTT",
                    Summary = "Hạn mức mua sắm cấp phòng tối đa 20 triệu VNĐ/lần; vượt hạn mức cần trình Ban Giám đốc.",
                    FileUrl = "/docs/quy-che-mua-sam-cntt.pdf",
                    Version = "1.0",
                    EffectiveDate = new DateTime(2026, 1, 1),
                    AccessLevel = "Restricted",
                    Status = "Active",
                    IndexStatus = "Indexed",
                    CreatedAt = now,
                    CategoryID = catHanhChinh.CategoryID,
                    IssuedByDepartmentID = deptHR.DepartmentID,
                    UploadedByUserID = adminUser.UserID
                };
                context.Documents.AddRange(docLeavePolicy, docPurchasePolicy);
                context.SaveChanges();

                context.DocumentRoleAccesses.AddRange(
                    // Tài liệu nghỉ phép: tất cả Role đều xem được
                    new DocumentRoleAccess { DocumentID = docLeavePolicy.DocumentID, RoleID = roleEmployee.RoleID },
                    new DocumentRoleAccess { DocumentID = docLeavePolicy.DocumentID, RoleID = roleManager.RoleID },
                    new DocumentRoleAccess { DocumentID = docLeavePolicy.DocumentID, RoleID = roleAdmin.RoleID },
                    // Tài liệu hạn mức mua sắm: chỉ Manager và Admin xem được
                    new DocumentRoleAccess { DocumentID = docPurchasePolicy.DocumentID, RoleID = roleManager.RoleID },
                    new DocumentRoleAccess { DocumentID = docPurchasePolicy.DocumentID, RoleID = roleAdmin.RoleID }
                );
                context.SaveChanges();

                // 8. RequestTypes, FormTemplates & WorkflowSteps
                var typeLeave = new RequestType { TypeCode = "Leave", TypeName = "Đơn xin nghỉ phép", IsActive = true };
                var typePurchase = new RequestType { TypeCode = "Purchase", TypeName = "Đề nghị mua sắm thiết bị", IsActive = true };
                var typeRoom = new RequestType { TypeCode = "RoomBooking", TypeName = "Đăng ký phòng họp", IsActive = true };
                context.RequestTypes.AddRange(typeLeave, typePurchase, typeRoom);
                context.SaveChanges();

                var tplLeave = new FormTemplate
                {
                    RequestTypeID = typeLeave.RequestTypeID,
                    Title = "Mẫu Đơn Xin Nghỉ Phép",
                    Version = "1.0",
                    FieldsJson = "[\"StartDate\", \"EndDate\", \"Reason\"]",
                    DownloadUrl = "/forms/mau-don-nghi-phep.docx",
                    Instructions = "Ghi rõ ngày bắt đầu, ngày kết thúc và lý do nghỉ phép.",
                    IsActive = true
                };
                var tplPurchase = new FormTemplate
                {
                    RequestTypeID = typePurchase.RequestTypeID,
                    Title = "Mẫu Phiếu Đề Nghị Mua Sắm",
                    Version = "1.0",
                    FieldsJson = "[\"ItemName\", \"Quantity\", \"EstimatedCost\", \"Reason\"]",
                    DownloadUrl = "/forms/mau-phieu-mua-sam.docx",
                    Instructions = "Nêu rõ tên thiết bị, số lượng, dự toán chi phí và mục đích sử dụng.",
                    IsActive = true
                };
                var tplRoom = new FormTemplate
                {
                    RequestTypeID = typeRoom.RequestTypeID,
                    Title = "Mẫu Đặt Phòng Họp",
                    Version = "1.0",
                    FieldsJson = "[\"RoomName\", \"MeetingDate\", \"StartTime\", \"EndTime\", \"Purpose\"]",
                    DownloadUrl = "/forms/mau-dat-phong-hop.docx",
                    Instructions = "Chọn phòng họp, khung giờ và nội dung cuộc họp.",
                    IsActive = true
                };
                context.FormTemplates.AddRange(tplLeave, tplPurchase, tplRoom);
                context.SaveChanges();

                var stepLeave1 = new WorkflowStep
                {
                    RequestTypeID = typeLeave.RequestTypeID,
                    StepOrder = 1,
                    StepName = "Trưởng phòng trực tiếp phê duyệt",
                    ApproverRoleID = roleManager.RoleID,
                    ApproverScope = "SameDepartment"
                };
                var stepLeave2 = new WorkflowStep
                {
                    RequestTypeID = typeLeave.RequestTypeID,
                    StepOrder = 2,
                    StepName = "Phòng Nhân sự xác nhận quỹ phép",
                    ApproverRoleID = roleEmployee.RoleID,
                    ApproverScope = "SpecificDepartment"
                };
                var stepPurchase1 = new WorkflowStep
                {
                    RequestTypeID = typePurchase.RequestTypeID,
                    StepOrder = 1,
                    StepName = "Trưởng phòng phê duyệt nhu cầu",
                    ApproverRoleID = roleManager.RoleID,
                    ApproverScope = "SameDepartment"
                };
                context.WorkflowSteps.AddRange(stepLeave1, stepLeave2, stepPurchase1);
                context.SaveChanges();

                // 9. Requests, RequestApprovals & RequestStatusHistories
                // Đơn 1: Đơn nghỉ phép đang Pending của dev1
                var reqPendingLeave = new Request
                {
                    Title = "Xin nghỉ phép đi khám sức khỏe",
                    Payload = "{\"StartDate\":\"2026-12-01\",\"EndDate\":\"2026-12-01\",\"Reason\":\"Khám sức khỏe định kỳ\"}",
                    CurrentStatus = "Pending",
                    CreatedAt = now.AddHours(-2),
                    UpdatedAt = now.AddHours(-2),
                    IdempotencyKey = "seed-req-leave-001",
                    UserID = dev1.UserID,
                    RequestTypeID = typeLeave.RequestTypeID,
                    TemplateID = tplLeave.TemplateID
                };

                // Đơn 2: Đơn nghỉ phép đã Approved của dev1
                var reqApprovedLeave = new Request
                {
                    Title = "Xin nghỉ phép việc gia đình",
                    Payload = "{\"StartDate\":\"2026-11-10\",\"EndDate\":\"2026-11-11\",\"Reason\":\"Giải quyết việc gia đình\"}",
                    CurrentStatus = "Approved",
                    CreatedAt = now.AddDays(-15),
                    UpdatedAt = now.AddDays(-14),
                    IdempotencyKey = "seed-req-leave-002",
                    UserID = dev1.UserID,
                    RequestTypeID = typeLeave.RequestTypeID,
                    TemplateID = tplLeave.TemplateID
                };

                // Đơn 3: Đơn mua sắm đang Pending của dev1
                var reqPendingPurchase = new Request
                {
                    Title = "Đề nghị cấp màn hình phụ phục vụ lập trình",
                    Payload = "{\"ItemName\":\"Màn hình Dell UltraSharp 27 inch\",\"Quantity\":1,\"EstimatedCost\":8500000,\"Reason\":\"Phục vụ phát triển hệ thống eOffice\"}",
                    CurrentStatus = "Pending",
                    CreatedAt = now.AddDays(-1),
                    UpdatedAt = now.AddDays(-1),
                    IdempotencyKey = "seed-req-purchase-001",
                    UserID = dev1.UserID,
                    RequestTypeID = typePurchase.RequestTypeID,
                    TemplateID = tplPurchase.TemplateID
                };

                context.Requests.AddRange(reqPendingLeave, reqApprovedLeave, reqPendingPurchase);
                context.SaveChanges();

                context.RequestApprovals.AddRange(
                    // Approvals cho Đơn 1 (Pending)
                    new RequestApproval { RequestID = reqPendingLeave.RequestID, StepOrder = 1, WorkflowStepID = stepLeave1.WorkflowStepID, Status = "Pending" },
                    new RequestApproval { RequestID = reqPendingLeave.RequestID, StepOrder = 2, WorkflowStepID = stepLeave2.WorkflowStepID, Status = "Pending" },
                    // Approvals cho Đơn 2 (Approved)
                    new RequestApproval { RequestID = reqApprovedLeave.RequestID, StepOrder = 1, WorkflowStepID = stepLeave1.WorkflowStepID, Status = "Approved", ActedAt = now.AddDays(-14).AddHours(-3), ActedByUserID = managerIT.UserID, Comment = "Đồng ý" },
                    new RequestApproval { RequestID = reqApprovedLeave.RequestID, StepOrder = 2, WorkflowStepID = stepLeave2.WorkflowStepID, Status = "Approved", ActedAt = now.AddDays(-14), ActedByUserID = hr1.UserID, Comment = "Đã trừ phép năm" },
                    // Approval cho Đơn 3 (Pending)
                    new RequestApproval { RequestID = reqPendingPurchase.RequestID, StepOrder = 1, WorkflowStepID = stepPurchase1.WorkflowStepID, Status = "Pending" }
                );

                context.RequestStatusHistories.AddRange(
                    new RequestStatusHistory { RequestID = reqPendingLeave.RequestID, ChangeNo = 1, OldStatus = null, NewStatus = "Pending", ChangedAt = now.AddHours(-2), ChangedByUserID = dev1.UserID, Note = "Tạo đơn mới" },
                    new RequestStatusHistory { RequestID = reqApprovedLeave.RequestID, ChangeNo = 1, OldStatus = null, NewStatus = "Pending", ChangedAt = now.AddDays(-15), ChangedByUserID = dev1.UserID, Note = "Tạo đơn mới" },
                    new RequestStatusHistory { RequestID = reqApprovedLeave.RequestID, ChangeNo = 2, OldStatus = "Pending", NewStatus = "Approved", ChangedAt = now.AddDays(-14), ChangedByUserID = hr1.UserID, Note = "Hoàn tất phê duyệt" },
                    new RequestStatusHistory { RequestID = reqPendingPurchase.RequestID, ChangeNo = 1, OldStatus = null, NewStatus = "Pending", ChangedAt = now.AddDays(-1), ChangedByUserID = dev1.UserID, Note = "Tạo đề nghị mua sắm" }
                );

                context.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}