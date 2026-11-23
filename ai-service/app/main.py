import logging
from contextlib import asynccontextmanager
from typing import NoReturn

from fastapi import Depends, FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse
from langchain_core.messages import HumanMessage
from langfuse import Langfuse
from langfuse.langchain import CallbackHandler

from app.clients import ToolResult, close_backend_client, get_backend_client
from app.config import settings
from app.core.security import UserContext, get_current_user
from app.graph.builder import graph
from app.schemas import ChatRequest, ChatResponse

log = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(_app):
    yield
    await close_backend_client()


app = FastAPI(title="AI Service", lifespan=lifespan)

# ==========================================
# Langfuse: khởi tạo client từ Settings
# ==========================================

langfuse_enabled = bool(settings.langfuse_public_key and settings.langfuse_secret_key)
if langfuse_enabled:
    Langfuse(
        public_key=settings.langfuse_public_key,
        secret_key=settings.langfuse_secret_key,
        host=settings.langfuse_host,
    )


def get_callbacks() -> list:
    return [CallbackHandler()] if langfuse_enabled else []


# ==========================================
# Helper
# ==========================================

SESSION_NOT_FOUND_MSG = "Không tìm thấy phiên trò chuyện."
GENERIC_ERROR_MSG = "Hệ thống gặp lỗi, vui lòng thử lại sau."

_STATUS_BY_CODE = {
    "VALIDATION_ERROR": 400,
    "UNAUTHORIZED": 401,
    "PERMISSION_DENIED": 403,
    "NOT_FOUND": 404,
}


def _raise(code: str, message: str) -> NoReturn:
    raise HTTPException(status_code=_STATUS_BY_CODE.get(code, 500),
                        detail={"code": code, "message": message})


def _raise_from(r: ToolResult, not_found_msg: str = SESSION_NOT_FOUND_MSG) -> NoReturn:
    """Đổi ToolResult lỗi của Backend thành HTTPException theo định dạng chung."""
    code = r.error.code if r.error else "INTERNAL_ERROR"
    if code == "NOT_FOUND":
        _raise(code, not_found_msg)          # phiên của người khác cũng rơi vào đây
    if code == "INTERNAL_ERROR":
        _raise(code, GENERIC_ERROR_MSG)
    _raise(code, r.error.message)


def _new_turn(question: str) -> dict:
    """Input cho 1 lượt: reset các trường của lượt, messages thì nối thêm."""
    return {
        "question": question,
        "intent": None,
        "tool_name": None,
        "tool_args": None,
        "tool_result": None,
        "answer": None,
        "messages": [HumanMessage(content=question)],
    }


# ==========================================
# API Endpoint
# ==========================================

@app.post("/chat", response_model=ChatResponse)
async def chat_endpoint(payload: ChatRequest,
                        user: UserContext = Depends(get_current_user)):
    question = payload.message.strip()
    if not question:
        _raise("VALIDATION_ERROR", "Nội dung câu hỏi không được để trống.")

    backend = get_backend_client()

    # 1. Chưa có phiên -> tạo mới qua Backend (tiêu đề = 50 ký tự đầu câu hỏi)
    session_id = payload.session_id
    if session_id is None:
        r = await backend.create_session(user.token, title=question[:50])
        if not r.success:
            _raise_from(r)
        session_id = int(r.data["session_id"])

    # 2. Lưu tin người dùng TRƯỚC khi chạy graph.
    #    Backend kiểm phiên có thuộc user trong token không -> phiên lạ trả 404 ở đây,
    #    nên graph không bao giờ chạy trên thread của người khác.
    r = await backend.save_message(user.token, session_id, "user", question)
    if not r.success:
        _raise_from(r)

    # 3. Chạy graph trên thread riêng của (user, phiên)
    run_config = {
        "configurable": {"thread_id": f"{user.user_id}:{session_id}", "user": user},
        "callbacks": get_callbacks(),
    }
    try:
        final_state = await graph.ainvoke(_new_turn(question), config=run_config)
    except Exception:
        log.exception("Lỗi chạy graph (user=%s, session=%s)", user.user_id, session_id)
        _raise("INTERNAL_ERROR", GENERIC_ERROR_MSG)

    answer = final_state.get("answer") or "Xin lỗi, hệ thống không thể tạo câu trả lời."
    intent = final_state.get("intent")
    trace_id = None            # TODO bước 4: lấy từ Langfuse
    sources: list = []         # Phase 2 (RAG)

    # 4. Lưu tin trả lời. Lỗi ở đây không làm mất câu trả lời: vẫn trả về, chỉ ghi log.
    message_id = None
    r = await backend.save_message(user.token, session_id, "assistant", answer,
                                   intent=intent, router_tier=None,
                                   sources=sources, trace_id=trace_id)
    if r.success:
        message_id = int(r.data["message_id"])
    else:
        log.error("Không lưu được tin trả lời (session=%s): %s", session_id, r.error)

    return ChatResponse(
        message_id=message_id,
        session_id=session_id,
        answer=answer,
        intent=intent,
        router_tier=None,
        sources=sources,
        trace_id=trace_id,
    )


@app.get("/health")
def health_check():
    return {"status": "ok"}


@app.exception_handler(HTTPException)
async def http_error_handler(request: Request, exc: HTTPException):
    error = exc.detail if isinstance(exc.detail, dict) else \
            {"code": "INTERNAL_ERROR", "message": str(exc.detail)}
    return JSONResponse(status_code=exc.status_code,
                        content={"success": False, "data": None, "error": error})