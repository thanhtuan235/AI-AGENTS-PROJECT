# Tool Contracts — AI Agent ↔ Backend

Tài liệu này định nghĩa "hợp đồng" giữa Agent (LangChain/LangGraph) và Backend (.NET 8 API). Mỗi tool được LLM gọi qua cơ chế function calling, sau đó code Python thực thi việc gọi API .NET thật.

> **Nguồn schema:** migration `20261003032336_AddFullEOfficeSchema`. Tên bảng/cột và giá trị enum dưới đây đã đối chiếu với schema và controller hiện có. Phần nào backend chưa hỗ trợ được đánh dấu rõ ở bảng "Trạng thái backend".


## Response Contract (áp dụng cho mọi tool)

Backend .NET hiện trả JSON theo từng endpoint (ví dụ `{ Message, RequestId, Status }` hoặc `{ Message }` khi lỗi), **không** theo format dưới đây. Lớp wrapper trong Python chịu trách nhiệm chuyển đổi về format chuẩn.

**Thành công:**
```json
{
  "success": true,
  "data": { "...": "tùy tool" },
  "error": null
}
```

**Thất bại:**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Thiếu trường bắt buộc: reason"
  }
}
```

**Danh sách `error.code` chuẩn hóa và ánh xạ từ HTTP status:**
- `VALIDATION_ERROR` — thiếu/sai dữ liệu đầu vào (HTTP 400)
- `NOT_FOUND` — request_id/resource không tồn tại (HTTP 404)
- `PERMISSION_DENIED` — user không có quyền xem/sửa resource đó (HTTP 403)
- `INTERNAL_ERROR` — lỗi phía backend .NET (HTTP 5xx)

> Backend chưa có authentication, nên việc kiểm tra "request này có phải của user này không" (cho `cancel_request`) phải do endpoint tự so `UserId` với chủ sở hữu và trả 403.

---

## Danh sách Tool

| #   | Tool                 | Mục đích                                             |
| --- | -------------------- | ---------------------------------------------------- |
| 1   | `create_request`     | Tạo yêu cầu mới (nghỉ phép/mua sắm/đặt phòng)        |
| 2   | `cancel_request`     | Hủy yêu cầu (chỉ khi đang `Pending`)                 |
| 3   | `get_request_status` | Tra cứu trạng thái 1 request theo ID                 |
| 4   | `list_my_requests`   | Liệt kê request của 1 user, lọc theo trạng thái/loại |
| 5   | `get_workflow_steps` | Xem chi tiết các bước duyệt của 1 request            |
| 6   | `search_documents`   | Tìm văn bản/quy định liên quan                       |
| 7   | `get_form_template`  | Lấy thông tin biểu mẫu theo loại request             |
| 8   | `submit_feedback`    | Ghi nhận feedback like/dislike                       |

## Trạng thái backend (tại thời điểm viết)

| Tool                 | Trạng thái backend                                                                                 |
| -------------------- | -------------------------------------------------------------------------------------------------- |
| `create_request`     | Chỉ có `POST /api/requests/leave`. Chưa có endpoint chung cho `Purchase`/`RoomBooking`             |
| `cancel_request`     | Chưa có endpoint, chưa có trạng thái `Cancelled`                                                   |
| `get_request_status` | **Sẵn sàng**: `GET /api/requests/{id}`                                                             |
| `list_my_requests`   | Chưa có endpoint (bảng `Requests` đã có `UserId`, `RequestType`, `Status`)                         |
| `get_workflow_steps` | Chưa có bảng `WorkflowSteps` và endpoint. Tạm thời chỉ có `RequestStatusHistories` (lịch sử đổi trạng thái) |
| `search_documents`   | **Sẵn sàng**: `GET /api/documents/search?keyword=` (khớp chuỗi con trên `Title`, `Summary`)        |
| `get_form_template`  | Có `GET /api/documents/forms?category=` nhưng chỉ trả `Id`, `Title`, `DownloadUrl`, **chưa có danh sách field** |
| `submit_feedback`    | Chưa có bảng `Feedback` và endpoint                                                                |

---

### 1. `create_request`

```json
{
  "name": "create_request",
  "description": "Tạo một yêu cầu mới (nghỉ phép, mua sắm, hoặc đặt phòng họp) cho user hiện tại. Chỉ gọi khi đã thu thập đủ thông tin bắt buộc từ cuộc hội thoại, không tự suy đoán giá trị còn thiếu. Nếu chưa chắc field nào bắt buộc cho loại request này, hãy gọi get_form_template trước.",
  "parameters": {
    "type": "object",
    "properties": {
      "request_type": {
        "type": "string",
        "enum": ["Leave", "Purchase", "RoomBooking"],
        "description": "Loại yêu cầu (khớp cột Requests.RequestType)"
      },
      "user_id": {
        "type": "integer",
        "description": "ID của user tạo yêu cầu (khớp Users.Id)"
      },
      "start_date": {
        "type": "string",
        "format": "date",
        "description": "Ngày bắt đầu, định dạng YYYY-MM-DD. Bắt buộc với Leave, RoomBooking"
      },
      "end_date": {
        "type": "string",
        "format": "date",
        "description": "Ngày kết thúc, định dạng YYYY-MM-DD. Bắt buộc với Leave"
      },
      "reason": {
        "type": "string",
        "description": "Lý do yêu cầu"
      },
      "extra_data": {
        "type": "object",
        "description": "Dữ liệu riêng theo loại request, backend lưu vào Requests.PayloadDetail (chuỗi JSON). Purchase: {item_name, quantity, estimated_cost}. RoomBooking: {room_id, time_slot, attendee_count}.",
        "properties": {
          "item_name": { "type": "string", "description": "Tên vật tư/thiết bị cần mua (Purchase)" },
          "quantity": { "type": "integer", "description": "Số lượng (Purchase)" },
          "estimated_cost": { "type": "number", "description": "Chi phí dự kiến (Purchase)" },
          "room_id": { "type": "string", "description": "Tên/mã phòng họp (RoomBooking)" },
          "time_slot": { "type": "string", "description": "Khung giờ, ví dụ '09:00-11:00' (RoomBooking)" },
          "attendee_count": { "type": "integer", "description": "Số người tham dự (RoomBooking)" }
        }
      }
    },
    "required": ["request_type", "user_id", "reason"]
  }
}
```

Ghi chú ánh xạ:
- `start_date`, `end_date`, `reason`, `extra_data` được gói vào `Requests.PayloadDetail` dạng JSON. Không cần thêm cột trong DB, chỉ cần endpoint chấp nhận `extra_data`.
- `Requests.Title` do backend tự sinh (ví dụ `Xin nghỉ phép: {FullName}`), agent không gửi.
- `Status` ban đầu luôn là `Pending`, và backend tự ghi 1 dòng `RequestStatusHistories` (`Draft` → `Pending`).
- Các field của `extra_data` là đề xuất, cần Người A xác nhận. Danh sách phòng họp chưa có bảng nên `room_id` tạm là chuỗi.
- Backend hiện nhận `StartDate`, `EndDate`, `Reason` là **chuỗi** qua `POST /api/requests/leave`.

**data khi thành công:** `{ "request_id": 12, "status": "Pending" }` (ánh xạ từ `RequestId`, `Status` của backend).

---

### 2. `cancel_request`

```json
{
  "name": "cancel_request",
  "description": "Hủy một yêu cầu đã tạo, chỉ thực hiện được khi yêu cầu đang ở trạng thái 'Pending' (chưa được duyệt). Nếu request đã 'ManagerApproved', 'Completed' hoặc 'Rejected', không được gọi tool này — hãy báo cho user biết lý do.",
  "parameters": {
    "type": "object",
    "properties": {
      "request_id": {
        "type": "integer",
        "description": "ID của request cần hủy (khớp Requests.Id)"
      },
      "user_id": {
        "type": "integer",
        "description": "ID user thực hiện hủy, dùng để xác thực quyền hủy đúng request của chính mình (khớp Users.Id)"
      },
      "cancel_reason": {
        "type": "string",
        "description": "Lý do hủy, không bắt buộc. Lưu vào RequestStatusHistories.Note"
      }
    },
    "required": ["request_id", "user_id"]
  }
}
```

Cần Người A thêm trạng thái `Cancelled` cho `Requests.Status`, và ghi 1 dòng `RequestStatusHistories` (`Pending` → `Cancelled`, `ChangedByUserId` = user_id).

---

### 3. `get_request_status`

```json
{
  "name": "get_request_status",
  "description": "Tra cứu trạng thái hiện tại của một yêu cầu theo ID. Dùng khi user hỏi 'yêu cầu của tôi tới đâu rồi', đã biết trước request_id cụ thể.",
  "parameters": {
    "type": "object",
    "properties": {
      "request_id": {
        "type": "integer",
        "description": "ID của request cần tra cứu (khớp Requests.Id)"
      }
    },
    "required": ["request_id"]
  }
}
```

**data khi thành công** (từ `GET /api/requests/{id}`):
```json
{
  "id": 1,
  "request_type": "Leave",
  "title": "Xin nghỉ phép đi khám bệnh",
  "status": "Pending",
  "created_at": "2026-10-03T03:23:36Z",
  "requester_name": "Trần Hữu Nguyên Sơn",
  "payload": "{\"StartDate\":\"2026-10-05\", ...}"
}
```
`payload` là chuỗi JSON thô lấy từ `PayloadDetail`, wrapper Python nên parse trước khi đưa cho LLM.

---

### 4. `list_my_requests`

```json
{
  "name": "list_my_requests",
  "description": "Liệt kê các yêu cầu của một user, có thể lọc theo trạng thái hoặc loại. Dùng khi user hỏi tổng quan 'tôi có những yêu cầu nào', không dùng để tra cứu 1 request cụ thể đã biết ID.",
  "parameters": {
    "type": "object",
    "properties": {
      "user_id": {
        "type": "integer",
        "description": "ID user cần liệt kê yêu cầu (khớp Users.Id)"
      },
      "status": {
        "type": "string",
        "enum": ["Pending", "ManagerApproved", "Rejected", "Completed", "Cancelled", "all"],
        "description": "Lọc theo trạng thái, mặc định 'all' nếu user không nói rõ. 'Cancelled' chỉ có sau khi backend thêm trạng thái này"
      },
      "request_type": {
        "type": "string",
        "enum": ["Leave", "Purchase", "RoomBooking", "all"],
        "description": "Lọc theo loại yêu cầu, mặc định 'all'"
      },
      "limit": {
        "type": "integer",
        "description": "Số lượng kết quả tối đa trả về, mặc định 20"
      }
    },
    "required": ["user_id"]
  }
}
```

**data mỗi phần tử:** `id`, `request_type`, `title`, `status`, `created_at` (không trả `payload` để gọn).

---

### 5. `get_workflow_steps`

```json
{
  "name": "get_workflow_steps",
  "description": "Xem chi tiết các bước duyệt (workflow) đã qua và còn lại của một request. Dùng khi user hỏi 'ai đang duyệt yêu cầu này', 'còn thiếu bước nào'. Khác get_request_status ở chỗ trả về chi tiết từng bước, không chỉ trạng thái tổng.",
  "parameters": {
    "type": "object",
    "properties": {
      "request_id": {
        "type": "integer",
        "description": "ID của request cần xem workflow (khớp Requests.Id)"
      }
    },
    "required": ["request_id"]
  }
}
```

Chờ Người A tạo bảng `WorkflowSteps` (`Id`, `RequestId`, `StepName`, `ApproverRole`, `Status`, `UpdatedAt`). Nếu A không làm kịp, phương án tạm là trả `RequestStatusHistories` (`OldStatus`, `NewStatus`, `Note`, `ChangedAt`), nhưng chỉ cho biết các bước **đã qua**, không có bước còn lại hay người duyệt. Khi đó phải sửa `description` ở trên để LLM không hứa điều tool không làm được.

---

### 6. `search_documents`

```json
{
  "name": "search_documents",
  "description": "Tìm kiếm văn bản/quy định nội bộ liên quan tới câu hỏi của user, ví dụ chính sách nghỉ phép, quy trình mua sắm. Dùng cho câu hỏi mang tính tra cứu thông tin/kiến thức, KHÔNG dùng để tạo hoặc tra trạng thái request. Truyền TỪ KHÓA ngắn (ví dụ 'nghỉ phép'), không truyền nguyên câu hỏi.",
  "parameters": {
    "type": "object",
    "properties": {
      "query": {
        "type": "string",
        "description": "Từ khóa ngắn cần tìm, khớp chuỗi con trong Documents.Title hoặc Documents.Summary"
      },
      "category": {
        "type": "string",
        "description": "Lọc theo Documents.Category nếu user nói rõ. Giá trị là mã KHÔNG DẤU, ví dụ 'NhanSu', 'HanhChinh'. // TODO: đổi thành enum khi seed đủ dữ liệu"
      },
      "top_k": {
        "type": "integer",
        "description": "Số kết quả tối đa trả về, mặc định 5. Backend chưa hỗ trợ nên Python cắt danh sách"
      }
    },
    "required": ["query"]
  }
}
```

**data mỗi phần tử** (từ `GET /api/documents/search?keyword=`): `id`, `title`, `category`, `summary`, `content_url`.

Lưu ý: endpoint hiện chỉ khớp chuỗi con (SQL `LIKE`), không hiểu ngữ nghĩa. Nếu agent truyền cả câu hỏi tự nhiên thì gần như không có kết quả. Nên để LLM trích từ khóa trước khi gọi.

---

### 7. `get_form_template`

```json
{
  "name": "get_form_template",
  "description": "Lấy thông tin biểu mẫu của một loại yêu cầu. Hiện trả về tên biểu mẫu và đường dẫn tải; khi backend bổ sung mô tả field thì sẽ trả cả danh sách field bắt buộc. Nên gọi tool này TRƯỚC create_request nếu chưa chắc cần hỏi user những thông tin gì.",
  "parameters": {
    "type": "object",
    "properties": {
      "form_type": {
        "type": "string",
        "enum": ["Leave", "Purchase", "RoomBooking"],
        "description": "Loại biểu mẫu, khớp FormTemplates.Category"
      }
    },
    "required": ["form_type"]
  }
}
```

**data mỗi phần tử** (từ `GET /api/documents/forms?category=`): `id`, `title`, `download_url`.

Thiếu: bảng `FormTemplates` không có cột mô tả field bắt buộc. Cần Người A thêm (ví dụ `FieldsJson`) thì tool mới đúng chức năng "cấu trúc biểu mẫu". Trong lúc chờ, danh sách field bắt buộc theo loại request có thể hardcode trong system prompt của agent.

---

### 8. `submit_feedback`

```json
{
  "name": "submit_feedback",
  "description": "Ghi nhận phản hồi (like/dislike) của user cho một câu trả lời của agent. Chỉ gọi khi user chủ động bấm nút feedback trên UI, không tự suy đoán feedback từ lời nói.",
  "parameters": {
    "type": "object",
    "properties": {
      "chat_session_id": {
        "type": "integer",
        "description": "ID phiên chat (khớp ChatSessions.Id)"
      },
      "message_id": {
        "type": "integer",
        "description": "ID tin nhắn được đánh giá (khớp ChatMessages.Id)"
      },
      "rating": {
        "type": "string",
        "enum": ["like", "dislike"]
      },
      "comment": {
        "type": "string",
        "description": "Ghi chú thêm nếu có, không bắt buộc"
      }
    },
    "required": ["chat_session_id", "message_id", "rating"]
  }
}
```

Chờ Người A tạo bảng `Feedback` (đề xuất: `Id`, `ChatSessionId`, `MessageId`, `Rating`, `Comment`, `CreatedAt`).

---

## Ghi chú triển khai

- `user_id` trong `create_request`, `cancel_request`, `list_my_requests` nên được **code Python gắn từ session đăng nhập**, không để LLM tự điền, vì model có thể bị dẫn dắt truyền `user_id` của người khác. Khi làm vậy có thể ẩn `user_id` khỏi schema gửi cho LLM.
- Mọi ID (`user_id`, `request_id`, `chat_session_id`, `message_id`) là `int`, đã khớp khóa chính trong DB.
- Giá trị `Status` và `RequestType` là chuỗi tự do (`nvarchar`), phân biệt theo đúng chữ viết hoa như trong enum ở trên.

## Mapping sang endpoint .NET

| Tool                 | Endpoint hiện có                         | Endpoint đề xuất (cần Người A)            |
| -------------------- | ---------------------------------------- | ----------------------------------------- |
| `create_request`     | `POST /api/requests/leave` (chỉ Leave)   | `POST /api/requests` (mọi loại)           |
| `cancel_request`     | —                                        | `PUT /api/requests/{id}/cancel`           |
| `get_request_status` | `GET /api/requests/{id}`                 | —                                         |
| `list_my_requests`   | —                                        | `GET /api/requests?userId=&status=&type=` |
| `get_workflow_steps` | —                                        | `GET /api/requests/{id}/workflow-steps`   |
| `search_documents`   | `GET /api/documents/search?keyword=`     | —                                         |
| `get_form_template`  | `GET /api/documents/forms?category=`     | `GET /api/forms/{type}` (kèm mô tả field) |
| `submit_feedback`    | —                                        | `POST /api/feedback`                      |

## Checklist còn lại

- [ ] Gửi Người A danh sách phần còn thiếu của DB/endpoint (xem bảng trạng thái backend ở trên)
- [ ] Chốt với A: giá trị `Status` cuối cùng (có `Cancelled` không), và tên enum `RequestType`
- [ ] Chốt field của `extra_data` cho `Purchase`, `RoomBooking`
- [ ] Đổi `category` của `search_documents` thành `enum` sau khi seed văn bản
- [ ] Viết wrapper Python chuyển response backend sang `{success, data, error}`
