# Text-to-SQL Design — Whitelist & Schema Description

> **Phạm vi sử dụng:** Tool này CHỈ phục vụ câu hỏi thống kê/tổng hợp (đếm, tính tổng, group by, so sánh theo thời gian). KHÔNG dùng để tra cứu chi tiết 1 bản ghi cụ thể — các tool `get_request_status`, `list_my_requests` đã đảm nhiệm việc đó.

> **Nguồn schema:** đối chiếu với migration `20261003032336_AddFullEOfficeSchema` (file `AppDbContextModelSnapshot.cs`). Khi Người A thêm migration mới, phải đọc lại snapshot và cập nhật tài liệu này.


## 1. Whitelist — Bảng/cột được phép truy vấn

### Nguyên tắc chọn lọc

- **Mặc định deny-all**: chỉ mở bảng/cột thực sự cần cho việc thống kê.
- Không expose cột chứa dữ liệu cá nhân (Email, FullName, Username), nội dung chat, ghi chú nội bộ (`Note`), nội dung JSON tự do (`PayloadDetail`).
- Không expose cột định danh người dùng (`UserId`, `ChangedByUserId`) để tránh join sâu vào bảng bị cấm.

### Bảng được phép (ALLOWED) — dùng được ngay với schema hiện tại

| Bảng                     | Cột được phép                          | Mục đích thống kê                                                                    |
| ------------------------ | -------------------------------------- | ------------------------------------------------------------------------------------ |
| `Requests`               | `Id`, `RequestType`, `Status`, `CreatedAt` | Đếm/group by theo loại, trạng thái, thời gian                                    |
| `RequestStatusHistories` | `RequestId`, `OldStatus`, `NewStatus`, `ChangedAt` | Thời gian chuyển trạng thái (vd: trung bình bao lâu 1 request được duyệt)    |
| `Documents`              | `Id`, `Title`, `Category`, `CreatedAt` | Thống kê số lượng văn bản theo danh mục/thời gian                                    |
| `FormTemplates`          | `Id`, `Title`, `Category`              | Đếm số biểu mẫu theo loại (`Category`)                                               |

Quan hệ join duy nhất được dùng: `RequestStatusHistories.RequestId = Requests.Id`.

> Lưu ý: `FormTemplates` chỉ là danh mục biểu mẫu, **không có liên kết** tới `Requests`, nên không thống kê được "mức độ sử dụng từng biểu mẫu". Muốn làm được cần thêm cột/bảng liên kết (xem bảng thiếu sót DB).

### Bảng/view CHỜ NGƯỜI A (chưa tồn tại — KHÔNG đưa vào prompt)

| Đối tượng        | Cột dự kiến                                                              | Mục đích                                           |
| ---------------- | ------------------------------------------------------------------------ | -------------------------------------------------- |
| `WorkflowSteps`  | `Id`, `RequestId`, `StepName`, `ApproverRole`, `Status`, `UpdatedAt`     | Thống kê bước nào bị nghẽn                         |
| `vw_RequestStats` (view) | `Id`, `RequestType`, `Status`, `CreatedAt`, `DepartmentId`, `DepartmentName` | Thống kê theo phòng ban mà không phải mở `Users` |

Đề xuất định nghĩa view:

```sql
CREATE VIEW vw_RequestStats AS
SELECT r.Id, r.RequestType, r.Status, r.CreatedAt,
       u.DepartmentId, d.Name AS DepartmentName
FROM Requests r
JOIN Users u ON u.Id = r.UserId
JOIN Departments d ON d.Id = u.DepartmentId;
```

Lý do dùng view: `Requests` **không có** cột `DepartmentId`; phòng ban nằm ở `Users.DepartmentId`. Mở thẳng `Users` sẽ lộ dữ liệu cá nhân, nên đi qua view đã ẩn cột nhạy cảm. Khi view có, thêm `vw_RequestStats` vào whitelist và vào prompt (không thêm `Users`, `Departments` gốc).

### Bảng KHÔNG được phép (FORBIDDEN)

| Bảng           | Lý do cấm                                                                                  |
| -------------- | ------------------------------------------------------------------------------------------ |
| `Users`        | Chứa thông tin cá nhân (`Email`, `FullName`, `Username`) và `Role`                          |
| `Departments`  | Chỉ cần qua view `vw_RequestStats`; `ManagerId` trỏ tới `Users` nên không mở trực tiếp      |
| `ChatSessions` | Chứa dữ liệu phiên làm việc gắn với `UserId`                                               |
| `ChatMessages` | Chứa nội dung hội thoại riêng tư của user                                                   |
| `Feedback`     | (Chưa tồn tại) Có thể chứa comment cá nhân, khi có bảng vẫn giữ cấm                         |

Ghi chú: schema hiện **không có bảng `Roles`**; vai trò là cột chuỗi `Users.Role` (Admin/Manager/Employee).

**Nguyên tắc xử lý khi cần thông tin từ bảng bị cấm:** tạo **view riêng** đã ẩn cột nhạy cảm, rồi thêm view đó (không phải bảng gốc) vào whitelist.

---

## 2. Schema Description — Đưa vào prompt cho LLM

```
Bạn được phép sinh câu lệnh T-SQL (SQL Server) dạng SELECT, chỉ đọc dữ liệu, trên các bảng sau.
KHÔNG được tham chiếu bất kỳ bảng hay cột nào ngoài danh sách này.

────────────────────────────────────────
Bảng: Requests
Mô tả: Các yêu cầu (nghỉ phép/mua sắm/đặt phòng) của nhân viên
Cột:
- Id (int, khóa chính)
- RequestType (nvarchar): 'Leave' | 'Purchase' | 'RoomBooking'
- Status (nvarchar): 'Pending' | 'ManagerApproved' | 'Rejected' | 'Completed'
- CreatedAt (datetime2): thời điểm tạo yêu cầu, lưu theo giờ UTC

────────────────────────────────────────
Bảng: RequestStatusHistories
Mô tả: Lịch sử thay đổi trạng thái của request, dùng để tính thời gian xử lý
Cột:
- RequestId (int, khóa ngoại → Requests.Id)
- OldStatus (nvarchar): cùng tập giá trị với Requests.Status, riêng trạng thái đầu có thể là 'Draft'
- NewStatus (nvarchar): cùng tập giá trị với Requests.Status
- ChangedAt (datetime2): thời điểm đổi trạng thái, giờ UTC

────────────────────────────────────────
Bảng: Documents
Mô tả: Văn bản/quy định nội bộ
Cột:
- Id (int, khóa chính)
- Title (nvarchar): tiêu đề văn bản
- Category (nvarchar): danh mục văn bản, ví dụ 'NhanSu', 'HanhChinh' (không dấu)
- CreatedAt (datetime2): thời điểm tạo bản ghi, giờ UTC

────────────────────────────────────────
Bảng: FormTemplates
Mô tả: Danh sách biểu mẫu
Cột:
- Id (int, khóa chính)
- Title (nvarchar): tên biểu mẫu
- Category (nvarchar): 'Leave' | 'Purchase' | 'RoomBooking'

────────────────────────────────────────
QUY TẮC BẮT BUỘC khi sinh SQL:
1. Chỉ được dùng câu lệnh SELECT — tuyệt đối không INSERT/UPDATE/DELETE/DROP/ALTER/EXEC.
2. Đây là SQL Server: dùng TOP 100 để giới hạn dòng (KHÔNG dùng LIMIT) nếu câu hỏi có thể trả về nhiều dòng.
3. Không dùng SELECT *; chỉ chọn cột cần thiết, và chỉ cột có trong danh sách trên.
4. Không tự bịa tên bảng/cột. Nếu không đủ thông tin để trả lời (ví dụ hỏi theo phòng ban, theo người tạo, theo số ngày nghỉ), hãy nói rõ không thể trả lời thay vì đoán bừa.
5. Ưu tiên hàm tổng hợp (COUNT, SUM, AVG, GROUP BY) cho câu hỏi thống kê, không trả về danh sách chi tiết từng dòng nếu không được yêu cầu rõ.
6. Thời gian lưu theo UTC. Khi nhóm theo ngày/tháng theo giờ Việt Nam, dùng DATEADD(HOUR, 7, CreatedAt) trước khi nhóm.
7. "Đã duyệt" được hiểu là Status IN ('ManagerApproved', 'Completed'); "đang chờ" là 'Pending'; "bị từ chối" là 'Rejected'.

Ví dụ:
Hỏi: "Trung bình mất bao nhiêu giờ để duyệt mỗi loại yêu cầu?"
SQL:
SELECT TOP 100 r.RequestType,
       AVG(DATEDIFF(HOUR, r.CreatedAt, h.ChangedAt)) AS AvgHoursToApprove
FROM Requests r
JOIN RequestStatusHistories h ON h.RequestId = r.Id
WHERE h.NewStatus = 'ManagerApproved'
GROUP BY r.RequestType
```

> Mục 7 là **giả định** về nghĩa của "đã duyệt" vì schema chưa có trạng thái `Approved` thuần. Cần xác nhận với Người A.

---

## 3. Tool JSON Schema — `text_to_sql_query`

```json
{
  "name": "text_to_sql_query",
  "description": "Chỉ dùng để trả lời câu hỏi thống kê/tổng hợp (đếm, tính tổng, group by, so sánh theo thời gian) trên dữ liệu Requests, RequestStatusHistories, Documents, FormTemplates. KHÔNG dùng tool này để tra cứu chi tiết 1 request cụ thể — hãy dùng get_request_status hoặc list_my_requests thay thế. Chỉ được sinh câu lệnh SELECT (T-SQL), không được tham chiếu bảng/cột ngoài whitelist đã cung cấp.",
  "parameters": {
    "type": "object",
    "properties": {
      "sql_query": {
        "type": "string",
        "description": "Câu lệnh T-SQL SELECT hợp lệ, chỉ dùng bảng/cột trong whitelist đã cung cấp trong system prompt"
      },
      "question_context": {
        "type": "string",
        "description": "Câu hỏi gốc của user, dùng để log lại phục vụ debug/audit"
      }
    },
    "required": ["sql_query", "question_context"]
  }
}
```

---

## 4. Lớp bảo vệ cần code ở giai đoạn implement

> Ghi chú  — không thuộc phạm vi tài liệu thiết kế lần này.

- [ ] Validate câu SQL chỉ bắt đầu bằng `SELECT`, chặn từ khóa `DROP/DELETE/UPDATE/INSERT/ALTER/EXEC`
- [ ] Validate tên bảng **và cột** trong câu SQL nằm trong whitelist (parse SQL, không chỉ check string chứa); chặn `SELECT *`
- [ ] Tự động thêm `TOP 100` nếu LLM quên giới hạn số dòng
- [ ] Dùng SQL Server login riêng cho agent, chỉ cấp quyền `SELECT` trên các bảng/view whitelist. Vì whitelist là theo **cột**, dùng `GRANT SELECT` mức cột hoặc truy vấn qua view, không cấp SELECT toàn bảng
- [ ] Log lại mọi câu SQL đã chạy (kèm `question_context`) để audit sau này

---

## Checklist còn lại

- [ ] Xác nhận với Người A: có thêm `WorkflowSteps`, view `vw_RequestStats` không
- [ ] Xác nhận nghĩa của "đã duyệt" (`ManagerApproved` + `Completed`) và có trạng thái `Cancelled` không
- [ ] Danh sách `Category` thật của `Documents` sau khi seed đủ 20–30 văn bản
- [ ] Kiểm tra lại toàn bộ tên cột khi có migration mới (timestamp lớn hơn `20261003032336`)
