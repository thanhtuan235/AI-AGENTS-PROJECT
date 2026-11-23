from typing import Any, Dict, Optional

from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse
from fastapi.exceptions import HTTPException
from fastapi import Depends, Header
from contextlib import asynccontextmanager
from app.clients import close_backend_client
from app.core.security import get_current_user, UserContext
from langchain_core.messages import HumanMessage
from langfuse import Langfuse
from langfuse.langchain import CallbackHandler
from pydantic import BaseModel
from app.schemas import ChatRequest

import logging
log = logging.getLogger(__name__)

from app.config import settings
from app.graph.builder import graph

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
# Schema Response
# ==========================================
class ChatResponse(BaseModel):
    answer: str
    intent: Optional[str] = None
    tool_name: Optional[str] = None
    tool_args: Optional[Dict[str, Any]] = None
    sources: list = []


# ==========================================
# API Endpoint
# ==========================================

@app.post("/chat", response_model=ChatResponse)
async def chat_endpoint(payload: ChatRequest,
                        user: UserContext = Depends(get_current_user)):
    question = payload.message.strip()
    if not question:
        raise HTTPException(status_code=400, detail="Nội dung câu hỏi không được để trống")

    # get_current_user đã kiểm chữ ký -> tới đây chắc chắn header hợp lệ
    turn_input = {
        "question": question,
        "intent": None,
        "tool_name": None,
        "tool_args": None,
        "tool_result": None,
        "answer": None,
        "messages": [HumanMessage(content=question)],
    }
    # TẠM (bước 3 sẽ thay bằng session_id thật từ Backend)
    thread_id = f"{user.user_id}:{payload.session_id or 'tmp'}"
    run_config = {
        "configurable": {"thread_id": thread_id, "user": user},
        "callbacks": get_callbacks(),
    }
    try:
        final_state = await graph.ainvoke(turn_input, config=run_config)
        return ChatResponse(
            answer=final_state.get("answer") or "Xin lỗi, hệ thống không thể tạo câu trả lời.",
            intent=final_state.get("intent"),
            tool_name=final_state.get("tool_name"),
            tool_args=final_state.get("tool_args"),
        )
    except Exception:
        log.exception("Lỗi xử lý /chat")
        raise HTTPException(status_code=500, detail="Hệ thống gặp lỗi, vui lòng thử lại sau.")


@app.get("/health")
def health_check():
    return {"status": "ok"}

@app.exception_handler(HTTPException)
async def http_error_handler(request: Request, exc: HTTPException):
    error = exc.detail if isinstance(exc.detail, dict) else \
            {"code": "INTERNAL_ERROR", "message": str(exc.detail)}
    return JSONResponse(status_code=exc.status_code,
                        content={"success": False, "data": None, "error": error})