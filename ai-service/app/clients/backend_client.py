import logging
from typing import Any, Optional
import httpx
from app.clients.types import ToolResult, NOT_FOUND_MSG

log = logging.getLogger(__name__)


class BackendClient:
    def __init__(self, base_url: str, timeout: float = 10.0,
                 transport: Optional[httpx.AsyncBaseTransport] = None):
        self._http = httpx.AsyncClient(base_url=base_url, timeout=timeout, transport=transport)

    async def aclose(self) -> None:
        await self._http.aclose()

    # ---------- lõi: gọi HTTP + chuẩn hóa ----------
    async def _call(self, method: str, path: str, token: str, **kw) -> ToolResult:
        headers = {"Authorization": f"Bearer {token}"}          # (a) forward token
        try:
            r = await self._http.request(method, path, headers=headers, **kw)
        except httpx.TimeoutException:                           # (b) timeout
            log.warning("Backend timeout: %s %s", method, path)
            return ToolResult.fail("INTERNAL_ERROR", "Hệ thống đang bận, vui lòng thử lại.")
        except httpx.HTTPError as e:                             # (c) mất kết nối
            log.error("Backend unreachable: %s %s (%s)", method, path, e)
            return ToolResult.fail("INTERNAL_ERROR", "Không kết nối được hệ thống.")

        s = r.status_code
        if s in (403, 404):                                      # (d) không lộ đơn tồn tại
            return ToolResult.fail("NOT_FOUND", NOT_FOUND_MSG)
        if s == 401:
            return ToolResult.fail("UNAUTHORIZED", "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.")
        if s == 400:
            return ToolResult.fail("VALIDATION_ERROR", _error_message(r) or "Dữ liệu không hợp lệ.")
        if s >= 500:                                             # (e) giấu chi tiết, ghi log
            log.error("Backend %s %s -> %s: %s", method, path, s, r.text[:500])
            return ToolResult.fail("INTERNAL_ERROR", "Hệ thống gặp lỗi, vui lòng thử lại sau.")

        body: Any = r.json() if r.content else None
        if isinstance(body, dict) and "success" in body:         # (f) bóc envelope
            if body["success"]:
                return ToolResult.ok(body.get("data"))
            err = body.get("error") or {}
            code = err.get("code", "INTERNAL_ERROR")
            if code in ("PERMISSION_DENIED", "NOT_FOUND"):
                return ToolResult.fail("NOT_FOUND", NOT_FOUND_MSG)
            return ToolResult.fail(code, err.get("message", "Có lỗi xảy ra."))
        return ToolResult.ok(body)

    # ---------- 3 hàm đọc cho tool ----------
    async def list_my_requests(self, token: str, status: Optional[str] = None,
                               request_type: Optional[str] = None) -> ToolResult:
        params = {}
        if status and status != "all":
            params["status"] = status
        if request_type and request_type != "all":
            params["type"] = request_type
        return await self._call("GET", "/api/requests", token, params=params)

    async def get_request(self, token: str, request_id: int) -> ToolResult:
        return await self._call("GET", f"/api/requests/{request_id}", token)

    async def get_form(self, token: str, form_type: str) -> ToolResult:
        return await self._call("GET", f"/api/forms/{form_type}", token)

    # ---------- 2 hàm phiên chat (A5 dùng) ----------
    async def create_session(self, token: str, title: Optional[str] = None) -> ToolResult:
        return await self._call("POST", "/api/chat/sessions", token, json={"title": title})

    async def save_message(self, token: str, session_id: int, role: str, content: str,
                           intent: Optional[str] = None, router_tier: Optional[str] = None,
                           sources: Optional[list] = None, trace_id: Optional[str] = None) -> ToolResult:
        return await self._call("POST", "/api/chat/messages", token, json={
            "session_id": session_id, "role": role, "content": content,
            "intent": intent, "router_tier": router_tier,
            "sources": sources or [], "trace_id": trace_id,
        })


def _error_message(r: httpx.Response) -> Optional[str]:
    try:
        b = r.json()
        return (b.get("error") or {}).get("message") or b.get("message")
    except Exception:
        return None