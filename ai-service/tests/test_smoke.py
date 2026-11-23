from fastapi.testclient import TestClient
from langchain_core.language_models.fake_chat_models import FakeListChatModel

import app.graph.nodes as nodes
from app.main import app

import time, jwt
from app.config import settings

def auth_header():
    token = jwt.encode(
        {"sub": "3", "role": "Employee", "dept_id": 1,
         "iss": settings.jwt_issuer, "aud": settings.jwt_audience,
         "exp": int(time.time()) + 3600},
        settings.jwt_key, algorithm="HS256",
    )
    return {"Authorization": f"Bearer {token}"}


def test_health():
    r = TestClient(app).get("/health")
    assert r.status_code == 200
    assert r.json()["status"] == "ok"


def test_chat_mock(monkeypatch):
    # LLM giả: lần 1 trả JSON intent, lần 2 trả câu trả lời → không cần Ollama
    replies = iter(['{"tool_name": "list_my_requests"}'])
    monkeypatch.setattr(
        nodes, "get_llm",
        lambda temperature=0.0: FakeListChatModel(responses=[next(replies)]),
    )

    r = TestClient(app).post("/chat",
                             json={"message": "danh sách đơn của tôi"},
                             headers=auth_header())

    assert r.status_code == 200, r.text
    body = r.json()
    assert body["answer"].startswith("Bạn có 2 đơn:")
    assert "#101" in body["answer"] and "#102" in body["answer"]