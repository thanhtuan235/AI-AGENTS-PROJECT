# AI Service

Service AI Agent sử dụng **FastAPI** + **LangChain/LangGraph**, kết nối với **Ollama** (model local) qua giao thức tương thích OpenAI API.

## Tech Stack

- **Python** (quản lý bằng `uv`)
- **FastAPI** — REST API framework
- **LangChain / LangGraph** — xây dựng agent flow
- **Langfuse** — tracing/observability cho LLM calls
- **Ollama** — chạy LLM local (model hiện dùng: `llama3`)

## Cấu trúc thư mục
    ai-service/
    ├── app/
    │ ├── init.py
    │ ├── main.py # FastAPI app, định nghĩa endpoint
    │ ├── config.py # Đọc biến môi trường từ .env
    │ └── llm.py # Cấu hình kết nối tới Ollama qua ChatOpenAI
    ├── .env # Biến môi trường (không commit)
    ├── pyproject.toml
    ├── uv.lock
    └── README.md

## Yêu cầu môi trường

- Python 3.11+
- [uv](https://docs.astral.sh/uv/) đã cài đặt
- [Ollama](https://ollama.com/) đã cài đặt và đang chạy (`ollama serve`)
- Model đã pull sẵn, ví dụ:
```bash
  ollama pull llama3
```

## Cài đặt

```bash
cd ai-service
uv venv
.venv\Scripts\activate      # Windows
# source .venv/bin/activate # Linux/Mac

uv sync
```

## Cấu hình

Tạo file `.env` ở thư mục gốc:

```env
OLLAMA_BASE_URL=http://localhost:11434/v1
OLLAMA_API_KEY=ollama
OLLAMA_MODEL=llama3

LANGFUSE_SECRET_KEY="sk-lf-c89ef21f-572a-48d6-bc57-a8e42abb18e7"
LANGFUSE_PUBLIC_KEY="pk-lf-752e77c3-148f-46dc-ad05-9a1d700cac1c"
LANGFUSE_BASE_URL="https://jp.cloud.langfuse.com"
```

## Chạy thử kết nối Ollama

```bash
uv run python -m app.llm
```

Nếu thấy phản hồi text từ model, kết nối đã thành công.

## Chạy server

```bash
uv run uvicorn app.main:app --reload --port 8001
```

Server chạy tại: `http://localhost:8001`

Swagger UI (test API trực tiếp trên trình duyệt): `http://localhost:8001/docs`

## API Endpoints

### `GET /health`

Kiểm tra service còn sống.

**Response:**
```json
{ "status": "ok", "service": "ai-service" }
```

### `POST /chat`

Gửi tin nhắn tới LLM (qua Ollama) và nhận phản hồi.

**Request body:**
```json
{ "message": "Xin chào" }
```

**Response:**
```json
{ "response": "Xin chào! Tôi có thể giúp gì cho bạn?" }
```

## Ghi chú

- Model mặc định: `llama3` — có thể đổi qua biến `OLLAMA_MODEL` trong `.env`.
- Port mặc định `8001` để tránh xung đột với `backend` (.NET, port 5000/5001) và `frontend` (Angular, port 4200).
- Không commit file `.env` (đã được `.gitignore` loại trừ).

## Roadmap (giai đoạn tiếp theo)

- [ ] Xây dựng LangGraph flow cho agent xử lý nghiệp vụ mock
- [ ] Kết nối HTTP với `backend` (.NET) để trao đổi dữ liệu
- [ ] Tích hợp Langfuse tracing đầy đủ cho các LLM call