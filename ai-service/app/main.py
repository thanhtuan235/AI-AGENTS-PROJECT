from typing import Any, Dict, Optional

from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse
from fastapi.exceptions import HTTPException
from fastapi import Depends
from app.core.security import get_current_user, UserContext
from langchain_core.messages import HumanMessage
from langfuse import Langfuse
from langfuse.langchain import CallbackHandler
from pydantic import BaseModel
import logging
logger = logging.getLogger(__name__)

from app.config import settings
from app.graph.builder import graph

app = FastAPI(title="AI Service")

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
# Schema Request / Response
# ==========================================

class ChatRequest(BaseModel):
    message: str
    session_id: Optional[str] = None
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
async def chat_endpoint(payload: ChatRequest, user: UserContext = Depends(get_current_user)):
    config = {"configurable": {"user_id": user.user_id, "role": user.role,
                               "dept_id": user.dept_id, "token": user.token}}
    
    question = payload.message.strip()
    if not question:
        raise HTTPException(status_code=400, detail="Nội dung câu hỏi không được để trống")

    initial_state = {
        "question": question,
        "messages": [HumanMessage(content=question)],
    }

    try:
        final_state = await graph.ainvoke(
            initial_state,
            config = {
                "callbacks": get_callbacks(),
                "configurable": {
                    "user_id": user.user_id,
                    "role": user.role,
                    "dept_id": user.dept_id,
                    "token": user.token,
                },
            }
        )
        return ChatResponse(
            answer=final_state.get("answer") or "Xin lỗi, hệ thống không thể tạo câu trả lời.",
            intent=final_state.get("intent"),
            tool_name=final_state.get("tool_name"),
            tool_args=final_state.get("tool_args"),
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Lỗi xử lý luồng AI Graph: {str(e)}")


@app.get("/health")
def health_check():
    return {"status": "ok"}

@app.exception_handler(HTTPException)
async def http_error_handler(request: Request, exc: HTTPException):
    error = exc.detail if isinstance(exc.detail, dict) else \
            {"code": "INTERNAL_ERROR", "message": str(exc.detail)}
    return JSONResponse(status_code=exc.status_code,
                        content={"success": False, "data": None, "error": error})