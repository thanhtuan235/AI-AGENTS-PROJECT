from typing import Any, Optional
from pydantic import BaseModel

NOT_FOUND_MSG = "Không tìm thấy yêu cầu phù hợp."

class ToolError(BaseModel):
    code: str      # VALIDATION_ERROR | NOT_FOUND | UNAUTHORIZED | INTERNAL_ERROR
    message: str

class ToolResult(BaseModel):
    success: bool
    data: Any = None
    error: Optional[ToolError] = None

    @classmethod
    def ok(cls, data: Any) -> "ToolResult":
        return cls(success=True, data=data)

    @classmethod
    def fail(cls, code: str, message: str) -> "ToolResult":
        return cls(success=False, error=ToolError(code=code, message=message))