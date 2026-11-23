import time

import jwt
from fastapi.testclient import TestClient
from langchain_core.language_models.fake_chat_models import FakeListChatModel

import app.graph.nodes as nodes
from app.config import settings
from app.graph.builder import graph
from app.main import app


def header(user_id: int = 3, role: str = "Employee", dept_id: int = 1) -> dict:
    token = jwt.encode(
        {"sub": str(user_id), "role": role, "dept_id": dept_id,
         "iss": settings.jwt_issuer, "aud": settings.jwt_audience,
         "exp": int(time.time()) + 3600},
        settings.jwt_key, algorithm="HS256",
    )
    return {"Authorization": f"Bearer {token}"}


def fake_llm(monkeypatch, replies):
    it = iter(replies)
    monkeypatch.setattr(nodes, "get_llm",
                        lambda temperature=0.0: FakeListChatModel(responses=[next(it)]))


def test_tao_phien_moi_va_tra_du_truong(monkeypatch):
    fake_llm(monkeypatch, ['{"tool_name": "list_my_requests"}', "Bạn có 2 đơn."])

    r = TestClient(app).post("/chat", json={"message": "danh sách đơn của tôi"}, headers=header())

    assert r.status_code == 200, r.text
    body = r.json()
    assert set(body) == {"message_id", "session_id", "answer", "intent",
                         "router_tier", "sources", "trace_id"}
    assert isinstance(body["session_id"], int)
    assert isinstance(body["message_id"], int)
    assert body["answer"] == "Bạn có 2 đơn."
    assert body["intent"] == "list_my_requests"
    assert body["router_tier"] is None and body["sources"] == []


def test_hai_luot_cung_phien_nho_ngu_canh(monkeypatch):
    fake_llm(monkeypatch, ['{"tool_name": "list_my_requests"}', "Lượt 1",
                           '{"tool_name": "chitchat"}', "Lượt 2"])
    client, h = TestClient(app), header()

    r1 = client.post("/chat", json={"message": "danh sách đơn của tôi"}, headers=h)
    sid = r1.json()["session_id"]
    r2 = client.post("/chat", json={"message": "cảm ơn", "session_id": sid}, headers=h)

    assert r2.json()["session_id"] == sid
    assert r1.json()["answer"] == "Lượt 1"
    assert r2.json()["answer"] == "Lượt 2"          # không lặp lại answer lượt 1

    snap = graph.get_state({"configurable": {"thread_id": f"3:{sid}"}})
    assert len(snap.values["messages"]) == 4        # 2 lượt x (Human + AI)
    token = h["Authorization"].removeprefix("Bearer ")
    assert token not in str(snap.values) and token not in str(snap.metadata)


def test_phien_cua_nguoi_khac_tra_404(monkeypatch):
    fake_llm(monkeypatch, ['{"tool_name": "chitchat"}', "Chào bạn"])
    client = TestClient(app)

    sid = client.post("/chat", json={"message": "xin chào"}, headers=header(user_id=3)).json()["session_id"]
    r = client.post("/chat", json={"message": "đơn của tôi", "session_id": sid},
                    headers=header(user_id=4))

    assert r.status_code == 404
    assert r.json()["error"]["code"] == "NOT_FOUND"
    # graph không chạy trên thread của user 4
    assert graph.get_state({"configurable": {"thread_id": f"4:{sid}"}}).values == {}


def test_cau_hoi_rong_tra_400():
    r = TestClient(app).post("/chat", json={"message": "   "}, headers=header())
    assert r.status_code == 400
    assert r.json()["error"]["code"] == "VALIDATION_ERROR"

def test_trace_id_khi_bat_langfuse(monkeypatch):
    import app.main as main
    from app.clients import get_backend_client

    fake_llm(monkeypatch, ['{"tool_name": "chitchat"}', "Chào bạn"])
    monkeypatch.setattr(main, "langfuse_enabled", True)

    # Bắt lại trace_id mà /chat gửi sang Backend khi lưu tin trả lời
    saved = {}
    backend = get_backend_client()
    original = backend.save_message

    async def spy(token, session_id, role, content, **kw):
        if role == "assistant":
            saved["trace_id"] = kw.get("trace_id")
        return await original(token, session_id, role, content, **kw)

    monkeypatch.setattr(backend, "save_message", spy)

    r = TestClient(app).post("/chat", json={"message": "xin chào"}, headers=header())

    trace_id = r.json()["trace_id"]
    assert isinstance(trace_id, str) and len(trace_id) == 32 
    assert saved["trace_id"] == trace_id                       

def test_thieu_token_tra_401_dinh_dang_chung():
    r = TestClient(app).post("/chat", json={"message": "xin chào"})
    assert r.status_code == 401
    assert r.json() == {"success": False, "data": None,
                        "error": {"code": "UNAUTHORIZED", "message": "Thiếu token."}}


def test_body_sai_tra_422_dinh_dang_chung():
    r = TestClient(app).post("/chat", json={"session_id": "abc"}, headers=header())
    assert r.status_code == 422
    body = r.json()
    assert body["success"] is False and body["error"]["code"] == "VALIDATION_ERROR"


def test_graph_loi_tra_500_thong_bao_chung(monkeypatch):
    def boom(temperature=0.0):
        raise RuntimeError("Ollama chết: connection refused 127.0.0.1:11434")
    monkeypatch.setattr(nodes, "get_llm", boom)

    r = TestClient(app).post("/chat", json={"message": "quy định công tác phí"}, headers=header())

    assert r.status_code == 500
    assert r.json()["error"] == {"code": "INTERNAL_ERROR",
                                 "message": "Hệ thống gặp lỗi, vui lòng thử lại sau."}
    assert "Ollama" not in r.text and "11434" not in r.text   # không lộ chi tiết


def test_swagger_co_nut_authorize():
    schema = TestClient(app).get("/openapi.json").json()
    assert "HTTPBearer" in schema["components"]["securitySchemes"]