# eOffice Chatbot Backend API (.NET 8)

Đây là Backend API System cung cấp dữ liệu CRUD, Database và Auth cho hệ thống eOffice Chatbot. 
Dự án được xây dựng bằng .NET 8 Web API và Entity Framework Core kết nối SQL Server.

## 🛠 Yêu cầu hệ thống (Prerequisites)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- SQL Server (Local hoặc Docker)

## 🚀 Hướng dẫn cài đặt và chạy dự án

**Bước 1: Clone code về máy**
\`\`\`bash
git clone <đường-link-repo-của-bạn>
cd backend
\`\`\`

**Bước 2: Cập nhật Connection String**
Mở file \`appsettings.json\` và sửa \`Server=...\` trong \`DefaultConnection\` cho khớp với tên SQL Server trên máy của bạn.

**Bước 3: Cài đặt EF Core Tool (Nếu máy bạn chưa có)**
Dành cho ai chạy lệnh migration bị lỗi \`CommandNotFound\`:
\`\`\`bash
dotnet tool install --global dotnet-ef
\`\`\`

**Bước 4: Khởi chạy dự án**
Mở terminal tại thư mục chứa file \`.csproj\` và chạy lệnh:
\`\`\`bash
dotnet run
\`\`\`
*(Lưu ý: Ứng dụng đã được tích hợp cơ chế tự động chạy Migration và Seed Data ảo. Bạn không cần gõ lệnh update database thủ công. Chỉ cần chạy \`dotnet run\`, DB và dữ liệu mẫu sẽ tự động được tạo!)*

## 📚 Tài liệu API (Swagger UI)
Sau khi ứng dụng chạy thành công, mở trình duyệt để xem và test toàn bộ API tại:
- **Swagger UI:** \`http://localhost:<port>/swagger\`
- **Health Check:** \`http://localhost:<port>/api/health\`
