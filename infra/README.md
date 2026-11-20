# Hạ tầng dev – eOffice Chatbot

Thư mục này dựng các thành phần hạ tầng dùng chung khi phát triển:

| Thành phần | Chạy ở đâu | Cổng |
|---|---|---|
| SQL Server 2025 (CU11) | Docker (`infra/docker-compose.yml`) | 1433 |
| Ollama (`qwen2.5:3b` + `bge-m3`) | Cài trực tiếp trên máy (khuyến nghị, dùng GPU) **hoặc** Docker với `--profile ollama` | 11434 |
| Langfuse | Langfuse Cloud (không chạy local) | — |

Các service ứng dụng (Angular, Chatbot Backend, eOffice Mock, AI Service) chạy trực tiếp trên máy, không nằm trong compose.

---

## 1. Yêu cầu

- Docker Desktop (Windows: backend WSL 2), `docker compose` v2.
- Docker được cấp ≥ 6 GB RAM (`docker info | findstr "Total Memory"`).
- Trống ~6 GB ổ đĩa (image SQL Server ~2,2 GB, 2 model ~3,1 GB).
- Cổng 1433 còn trống: `netstat -ano | findstr ":1433"` không có dòng `LISTENING`.
  Nếu máy có SQL Server cài sẵn đang chiếm 1433 → tắt service đó, hoặc đặt `MSSQL_PORT=14333` trong `.env`.

## 2. Cấu hình `.env`

Tại **thư mục gốc repo**:

```powershell
Copy-Item .env.example .env
```

Mở `.env` và điền:

- `MSSQL_SA_PASSWORD`: ≥ 8 ký tự, có hoa, thường, số, ký tự đặc biệt.
  **Không dùng** `$ # ; " '` hoặc dấu cách. Nên dùng `_ @ ! % -`.
  Sửa luôn phần `Password=` trong 2 dòng `ConnectionStrings__...`.
- `JWT_KEY`: nhận từ Thành (không tự sinh khóa riêng, nếu không token sẽ không khớp giữa các service).
- `LANGFUSE_*`: lấy Thành.

Trên Windows, đảm bảo `.env` dùng xuống dòng **LF** (góc dưới phải VS Code), nếu không mật khẩu có thể dính ký tự `\r` ẩn.

`.env` đã nằm trong `.gitignore` – **không commit**.

## 3. Chạy SQL Server

Mọi lệnh chạy từ **thư mục gốc repo**:

```powershell
docker compose -f infra/docker-compose.yml --env-file .env up -d
docker compose -f infra/docker-compose.yml --env-file .env ps
```

Lần đầu tải image vài phút. Chờ đến khi STATUS là `Up ... (healthy)` (30–90 giây) rồi mới kết nối.

Kiểm tra trong container:

```powershell
docker exec -it eoffice-sql bash
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT @@VERSION"
exit
```

Kết nối từ máy (SSMS / Azure Data Studio / VS Code extension **SQL Server (mssql)**):

| Trường | Giá trị |
|---|---|
| Server | `localhost`, port `1433` (SSMS: `localhost,1433`) |
| Authentication | SQL Login, user `sa` |
| Trust server certificate | Bật |

`SELECT @@SERVERNAME` phải trả `eoffice-sql`.

Database `EOfficeMockDb` và `ChatbotDb` do Backend tạo bằng EF migrations – không tạo tay.

## 4. Chạy Ollama

### Cách A – Cài trên máy (khuyến nghị)

Cài Ollama từ trang chủ, rồi:

```powershell
ollama pull qwen2.5:3b
ollama pull bge-m3
ollama list
```

Có GPU NVIDIA thì Ollama tự dùng. Kiểm tra bằng `ollama ps` (cột PROCESSOR).

### Cách B – Chạy bằng Docker (máy không cài Ollama)

```powershell
docker compose -f infra/docker-compose.yml --env-file .env --profile ollama up -d
docker exec -it eoffice-ollama ollama pull qwen2.5:3b
docker exec -it eoffice-ollama ollama pull bge-m3
```

Cách này chạy bằng CPU, chậm hơn đáng kể. Không chạy cùng lúc với Ollama cài trên máy (trùng cổng 11434).

### Kiểm tra

```powershell
# Embedding: phải in 1024
$body = @{ model = "bge-m3"; input = "Quy định nghỉ phép năm 2026" } | ConvertTo-Json
$r = Invoke-RestMethod -Uri http://localhost:11434/api/embed -Method Post `
     -Body ([Text.Encoding]::UTF8.GetBytes($body)) -ContentType "application/json; charset=utf-8"
$r.embeddings[0].Count

# LLM qua endpoint OpenAI-compatible (đường AI Service dùng)
$body = @{ model = "qwen2.5:3b"; temperature = 0;
           messages = @(@{ role = "user"; content = "Chào bạn" }) } | ConvertTo-Json -Depth 5
$r = Invoke-RestMethod -Uri http://localhost:11434/v1/chat/completions -Method Post `
     -Body ([Text.Encoding]::UTF8.GetBytes($body)) -ContentType "application/json; charset=utf-8"
$r.choices[0].message.content
```

PowerShell 5.1 có thể hiển thị tiếng Việt thành `ChÃ o báº¡n` – đó chỉ là lỗi hiển thị của terminal, dữ liệu trả về vẫn đúng UTF-8.

## 5. Lệnh thường dùng

| Việc | Lệnh |
|---|---|
| Dừng (giữ dữ liệu) | `docker compose -f infra/docker-compose.yml --env-file .env down` |
| **Xóa sạch dữ liệu** và khởi tạo lại | `docker compose -f infra/docker-compose.yml --env-file .env down -v` |
| Xem log SQL Server | `docker logs eoffice-sql --tail 40` |

## 6. Lỗi thường gặp

| Triệu chứng | Nguyên nhân | Cách xử lý |
|---|---|---|
| Container tự tắt / `Restarting` | Mật khẩu SA không đạt policy | Xem `docker logs eoffice-sql`, đổi mật khẩu, `down -v` rồi `up -d` |
| `Login failed for user 'sa'` | Mật khẩu gõ khác mật khẩu container giữ: `.env` có `\r`/dấu cách ẩn, hoặc đã đổi `.env` **sau** lần chạy đầu (SA chỉ được đặt khi khởi tạo volume) | Sửa `.env` (LF, bỏ khoảng trắng), `down -v` rồi `up -d` |
| `error occurred during the pre-login handshake` | SQL Server chưa khởi động xong | Chờ `ps` báo `(healthy)` rồi kết nối lại |
| `certificate chain was issued by an authority that is not trusted` | Chưa bật Trust server certificate | Bật tùy chọn này |
| `@@SERVERNAME` không phải `eoffice-sql` | Đang kết nối vào SQL Server cài sẵn trên máy | Tắt service đó hoặc đổi `MSSQL_PORT` |
| `sqlcmd` báo `Incorrect syntax` khi chạy qua `docker exec ... bash -c '...'` trên PowerShell | PowerShell làm hỏng dấu nháy lồng nhau | Vào `docker exec -it eoffice-sql bash` rồi chạy lệnh bên trong |

## 7. Ghi chú kỹ thuật

- **Full-Text Search chưa có** trong image SQL Server chính thức (`FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 0`). Vector search (`VECTOR`, `VECTOR_DISTANCE`) không bị ảnh hưởng. Phương án xử lý full-text quyết định ở task A6: build image có `mssql-server-fts`, hoặc BM25 trong Python.
- Image được ghim ở `2025-CU11-ubuntu-24.04` để cả nhóm dùng cùng một bản. Muốn nâng CU: sửa tag, cả nhóm cùng `pull`.
- `bge-m3` cho vector **1024 chiều** – khớp `EMBEDDING_DIM` và cột `VECTOR(1024)`. Đổi model embedding nghĩa là phải đổi schema và nhúng lại toàn bộ dữ liệu.
- SQL Server bị giới hạn 2 GB RAM (`MSSQL_MEMORY_LIMIT_MB`).