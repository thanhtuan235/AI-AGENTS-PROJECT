from typing import Any, Optional
from pydantic import BaseModel, Field


class ChatRequest(BaseModel):
    message: str = Field(min_length=1, max_length=4000)
    # Id phiên bên Backend là int. None = tạo phiên mới
    session_id: Optional[int] = None


class ChatResponse(BaseModel):
    """Theo hợp đồng A5: /chat trả thẳng object, không bọc envelope."""
    message_id: Optional[int] = None      # id tin trả lời đã lưu ở Backend
    session_id: int
    answer: str
    intent: Optional[str] = None
    router_tier: Optional[str] = None     # null tới Phase 1
    sources: list[Any] = Field(default_factory=list)  # [] tới Phase 2
    trace_id: Optional[str] = None