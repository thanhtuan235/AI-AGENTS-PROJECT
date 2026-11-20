from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Any, Dict, Optional
from langchain_core.messages import HumanMessage
from langfuse.langchain import CallbackHandler
from app.graph.builder import graph

app = FastAPI(title="AI Service")

langfuse_handler = CallbackHandler()

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
async def chat_endpoint(payload: ChatRequest):
    question = payload.message.strip()
    if not question:
        raise HTTPException(status_code=400, detail="Nội dung câu hỏi không được để trống")

    initial_state = {
        "question": question,
        "messages": [HumanMessage(content=question)],
    }

    try:
        final_state = graph.invoke(initial_state,config={"callbacks": [langfuse_handler]})

        return ChatResponse(
            answer=final_state.get("answer") or "Xin lỗi, hệ thống không thể tạo câu trả lời.",
            intent=final_state.get("intent"),
            tool_name=final_state.get("tool_name"),
            tool_args=final_state.get("tool_args"),
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Lỗi xử lý luồng AI Graph: {str(e)}")


# Health check endpoint
@app.get("/health")
def health_check():
    return {"status": "ok"}


# Vấn đề trong các app eOffice -> Lý do xuất hiện , Động lực có chatbot
# Các bên lớn làm chatbot như nào + tham khảo
# Cơ chế tích hợp vào hệ thống

# module chính: hướng dẫn câu hỏi thường gặp (câu hỏi cứng), trả lời từ các tài liệu, ra lệnh cho chatbot lập yêu cầu -> AI tự vào DB làm (Lập, tạo, sửa tờ đơn,...), 
# Quản lý các tài liệu, Quản lý các câu hỏi đáp, Quản lý trong db (Text-to-SQL) query
# Truy xuất có phân quyền
