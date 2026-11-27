# eOffice Administrative Chatbot — Backend (.NET 10)

Hệ thống Backend cho trợ lý ảo hành chính eOffice, được tổ chức theo kiến trúc **2 Services / 2 Databases** trong cùng solution `EOfficeChatbot.sln`:

1. **`EOffice.Mock` (Port `5200`):** Giả lập hệ thống lõi eOffice (`EOfficeMockDb` — 14 bảng nghiệp vụ Nhân sự, Phân quyền, Metadata Tài liệu, Quy trình phê duyệt) và đóng vai trò Identity Provider cấp phát JWT Token.
2. **`eOfficeChatbot.Api` (Port `5100`):** Backend chính của Chatbot (`ChatbotDb` — quản lý phiên hội thoại, tin nhắn, đánh giá phản hồi và cấu hình AI).

---

## 🛠 Yêu cầu hệ thống (Prerequisites)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- SQL Server (Docker chạy ở port `1433` hoặc SQL Server Local)
- EF Core CLI Tool (nếu cần thao tác migration thủ công):
  ```bash
  dotnet tool install --global dotnet-ef
  ```

---

## 🚀 Hướng dẫn cài đặt và chạy dự án

### Bước 1: Cấu hình chuỗi kết nối Database
Mặc định `appsettings.json` trỏ tới SQL Server Docker (`localhost,1433`). Nếu sử dụng SQL Server Local (Windows Authentication), hãy ghi đè bằng `user-secrets` để tránh làm thay đổi file cấu hình chung trên Git:

```bash
cd EOffice.Mock
dotnet user-secrets set "ConnectionStrings:EofficeMockDb" "Server=localhost;Database=EOfficeMockDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

### Bước 2: Khởi chạy `EOffice.Mock` (Port 5200)
Tại thư mục `EOffice.Mock`, chạy lệnh:
```bash
dotnet run
```
> Ứng dụng tự động chạy `Database.Migrate()` và `DbSeeder.Seed()` khi khởi động. Toàn bộ 14 bảng của `EOfficeMockDb` cùng dữ liệu mẫu (phòng ban, phân quyền, văn bản, biểu mẫu, đơn từ) sẽ được khởi tạo tự động.

- **Swagger UI:** `http://localhost:5200/swagger`

### Bước 3: Khởi chạy `eOfficeChatbot.Api` (Port 5100)
Mở một terminal khác tại thư mục `eOfficeChatbot.Api` và chạy:
```bash
dotnet run
```
- **Swagger UI:** `http://localhost:5100/swagger`

---

## 🔑 Tài khoản mẫu & Hợp đồng JWT (Dành cho FE & AI)

### Danh sách tài khoản Dev (Mật khẩu chung: `123456`)
| Username | Họ tên | Role (`role` claim) | Phòng ban (`dept_id`) | Phạm vi quyền |
| :--- | :--- | :--- | :--- | :--- |
| `admin` | Trần Minh Quản | `Admin` | `1` (IT) | Toàn quyền quản trị biểu mẫu, tài liệu, hệ thống |
| `truongphong.it` | Nguyễn Văn Trưởng | `Manager` | `1` (IT) | Duyệt đơn & xem thống kê phòng IT |
| `dev1` | Lê Hoàng An | `Employee` | `1` (IT) | Tạo/hủy/tra cứu đơn cá nhân, đọc tài liệu nội bộ |
| `truongphong.hr` | Phạm Thị Lan | `Manager` | `2` (HR) | Duyệt đơn & xem thống kê phòng HR |
| `hr1` | Đỗ Ngọc Mai | `Employee` | `2` (HR) | Nhân viên phòng Nhân sự - Hành chính |

### Cấu hình JWT (`HS256`)
- **Endpoint đăng nhập:** `POST http://localhost:5200/auth/login`
- **Issuer:** `eoffice-mock` | **Audience:** `eoffice-chatbot` | **Expire:** `60 phút`
- **Claims:** `sub` (`user_id`), `name`, `role`, `dept_id`

---

## 📌 Các Endpoint chính trên `EOffice.Mock` (`5200`)
- `POST /auth/login` — Đăng nhập nhận `access_token`
- `GET /auth/me` — Kiểm tra token & lấy thông tin hồ sơ hiện tại
- `GET /api/requests` — Danh sách đơn của người dùng hiện tại
- `GET /api/requests/{id}` — Chi tiết đơn, các bước duyệt (`Approvals`) và lịch sử trạng thái
- `POST /api/requests` — Tạo yêu cầu mới (tự động kiểm tra `FieldsJson` & chống trùng qua `IdempotencyKey`)
- `PUT /api/requests/{id}/cancel` — Hủy đơn đang ở trạng thái `Pending`
- `PUT /api/requests/{id}/approve` — Phê duyệt / từ chối đơn (chỉ `Manager` / `Admin`)
- `GET /api/documents` & `GET /api/documents/authorized-ids` — Tra cứu văn bản & lấy danh sách ID hợp lệ theo RBAC cho Vector DB
- `GET /api/documents/forms` & `POST /api/documents/forms` — Lấy danh sách biểu mẫu (`FieldsJson`) và tạo biểu mẫu mới (`Admin`)
