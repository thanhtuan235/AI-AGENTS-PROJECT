import itertools
from typing import Optional
from app.clients.types import ToolResult, NOT_FOUND_MSG
import jwt

_REQUESTS = [
    {"id": 101, "request_type": "Leave", "title": "Đơn xin nghỉ phép",
     "status": "Approved", "created_at": "2026-10-01T08:00:00Z"},
    {"id": 102, "request_type": "Purchase", "title": "Mua màn hình phụ",
     "status": "Pending", "created_at": "2026-10-06T09:30:00Z"},
]
_ids = itertools.count(1)

_session_owner: dict[int, str] = {}
def _sub(token: str) -> str:
    """Mock chỉ cần biết ai gọi; chữ ký đã được core/security.py kiểm trước đó."""
    return str(jwt.decode(token, options={"verify_signature": False})["sub"])

class MockBackendClient:
    async def aclose(self) -> None: ...

    async def list_my_requests(self, token, status=None, request_type=None):
        rows = [r for r in _REQUESTS
                if (not status or status == "all" or r["status"] == status)
                and (not request_type or request_type == "all" or r["request_type"] == request_type)]
        return ToolResult.ok(rows)

    async def get_request(self, token, request_id: int):
        for r in _REQUESTS:
            if r["id"] == request_id:
                return ToolResult.ok({**r, "requester_name": "Nhân viên Test",
                                      "payload": {"reason": "Việc gia đình"}})
        return ToolResult.fail("NOT_FOUND", NOT_FOUND_MSG)

    async def get_form(self, token, form_type: str):
        return ToolResult.ok({"id": 1, "title": f"Mẫu đơn {form_type}", "category": form_type,
                              "download_url": f"https://eoffice.local/templates/{form_type.lower()}.docx",
                              "fields": []})

    async def create_session(self, token, title: Optional[str] = None):
        session_id = next(_ids)
        _session_owner[session_id] = _sub(token)
        return ToolResult.ok({"session_id": session_id})

    async def save_message(self, token, session_id, role, content, **kw):
        if _session_owner.get(session_id) != _sub(token):
            return ToolResult.fail("NOT_FOUND", "Không tìm thấy phiên trò chuyện.")
        return ToolResult.ok({"message_id": next(_ids)})