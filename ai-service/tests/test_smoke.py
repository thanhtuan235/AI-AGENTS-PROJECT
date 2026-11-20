from fastapi.testclient import TestClient
from langchain_core.language_models.fake_chat_models import FakeListChatModel

import app.graph.nodes as nodes
from app.main import app


def test_health():
    r = TestClient(app).get("/health")
    assert r.status_code == 200
    assert r.json()["status"] == "ok"


def test_chat_mock(monkeypatch):
    # LLM giả: lần 1 trả JSON intent, lần 2 trả câu trả lời → không cần Ollama
    replies = iter(['{"tool_name": "list_my_requests"}', "Bạn có 2 đơn."])
    monkeypatch.setattr(
        nodes, "get_llm",
        lambda temperature=0.0: FakeListChatModel(responses=[next(replies)]),
    )

    r = TestClient(app).post("/chat", json={"message": "danh sách đơn của tôi"})

    assert r.status_code == 200, r.text
    body = r.json()
    assert body["intent"] == "list_my_requests"
    assert body["answer"] == "Bạn có 2 đơn."