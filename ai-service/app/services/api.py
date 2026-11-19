# app/services/office_api.py
import os
from typing import Dict, Any, Optional
import httpx
from app.config import settings

USE_MOCK = settings.use_mock
BASE_URL = settings.chatbot_backend_url

class OfficeService:
    def __init__(self, base_url: str = BASE_URL):
        self.client = httpx.Client(base_url=base_url, timeout=10.0)

    # 1. RAG / Documents
    def search_documents(self, query: str, top_k: int = 5) -> Dict[str, Any]:
        if USE_MOCK:
            return {
                "status": "success",
                "docs": [
                    {"title": "Quy chế nghỉ phép 2026", "content": "Nhân viên có 12 ngày phép năm. Nghỉ trên 3 ngày cần duyệt trước 1 tuần."},
                    {"title": "Quy định thanh toán", "content": "Hóa đơn mua sắm vật tư phải xuất đúng MST công ty."}
                ]
            }
        # return self.client.get("/api/v1/docs/search", params={"query": query, "top_k": top_k}).json()

    # 2. Get Request Status
    def get_request_status(self, request_id: Optional[int]) -> Dict[str, Any]:
        if USE_MOCK:
            return {
                "status": "success",
                "request_id": request_id or 101,
                "request_type": "Leave",
                "state": "PENDING_APPROVAL",
                "current_approver": "Trưởng phòng Kỹ thuật",
                "created_at": "2026-10-05"
            }
        # return self.client.get(f"/api/v1/requests/{request_id}/status").json()

    # 3. Create Request
    def create_request(
        self,
        request_type: str,
        start_date: Optional[str] = None,
        end_date: Optional[str] = None,
        reason: Optional[str] = None,
        extra_data: Optional[Dict[str, Any]] = None
    ) -> Dict[str, Any]:
        payload = {
            "request_type": request_type,
            "start_date": start_date,
            "end_date": end_date,
            "reason": reason,
            "extra_data": extra_data or {}
        }
        if USE_MOCK:
            return {
                "status": "success",
                "request_id": 888,
                "data": payload,
                "message": "Yêu cầu đã được khởi tạo thành công vào hệ thống eOffice."
            }
        # return self.client.post("/api/v1/requests", json=payload).json()

    # 4. Get Form Template
    def get_form_template(self, form_type: str) -> Dict[str, Any]:
        if USE_MOCK:
            return {
                "status": "success",
                "form_type": form_type,
                "download_url": f"https://eoffice.local/templates/{form_type.lower()}_template.docx"
            }
        # return self.client.get(f"/api/v1/templates/{form_type}").json()

    # 5. List Requests
    def list_my_requests(self, limit: int = 20, status: str = "all", request_type: str = "all") -> Dict[str, Any]:
        if USE_MOCK:
            return {
                "status": "success",
                "total": 2,
                "requests": [
                    {"id": 101, "title": "Đơn xin nghỉ phép", "status": "APPROVED", "date": "2026-10-01"},
                    {"id": 102, "title": "Mua màn hình phụ", "status": "PENDING", "date": "2026-10-06"}
                ]
            }
        # return self.client.get("/api/v1/requests/me", params={"limit": limit, "status": status, "request_type": request_type}).json()

    # 6. Cancel Request
    def cancel_request(self, request_id: Optional[int], cancel_reason: Optional[str] = None) -> Dict[str, Any]:
        if USE_MOCK:
            return {
                "status": "success",
                "request_id": request_id,
                "message": f"Đơn số #{request_id} đã được hủy thành công."
            }
        # return self.client.post(f"/api/v1/requests/{request_id}/cancel", json={"reason": cancel_reason}).json()

    # 7. Get Workflow Steps
    def get_workflow_steps(self, request_id: Optional[int]) -> Dict[str, Any]:
        if USE_MOCK:
            return {
                "status": "success",
                "request_id": request_id or 101,
                "steps": [
                    {"step": 1, "role": "Direct Lead", "status": "APPROVED", "comment": "Đồng ý cho nghỉ"},
                    {"step": 2, "role": "HR Manager", "status": "IN_REVIEW", "comment": "Chờ đối soát ngày phép"}
                ]
            }
        # return self.client.get(f"/api/v1/requests/{request_id}/workflow").json()

api = OfficeService()