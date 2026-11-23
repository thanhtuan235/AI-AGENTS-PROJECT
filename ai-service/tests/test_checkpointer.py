from fastapi.testclient import TestClient
from langchain_core.language_models.fake_chat_models import FakeListChatModel

import app.graph.nodes as nodes
from app.graph.builder import graph
from app.main import app
from tests.test_smoke import auth_header


def _fake_llm(monkeypatch, replies):
    it = iter(replies)
    monkeypatch.setattr(nodes, "get_llm",
                        lambda temperature=0.0: FakeListChatModel(responses=[next(it)]))


def test_hai_luot_cung_thread(monkeypatch):
    _fake_llm(monkeypatch, ['{"tool_name": "list_my_requests"}', "Lượt 1",
                            '{"tool_name": "chitchat"}', "Lượt 2"])
    client, h = TestClient(app), auth_header()

    r1 = client.post("/chat", json={"message": "danh sách đơn của tôi", "session_id": 777}, headers=h)
    r2 = client.post("/chat", json={"message": "cảm ơn", "session_id": 777}, headers=h)

    # Lượt 2 phải có câu trả lời MỚI, không lặp lại answer của lượt 1
    assert r1.json()["answer"] == "Lượt 1"
    assert r2.json()["answer"] == "Lượt 2"

    snap = graph.get_state({"configurable": {"thread_id": "3:777"}})
    # 2 lượt x (Human + AI) = 4 tin trong lịch sử
    assert len(snap.values["messages"]) == 4
    # Token không bị lưu vào checkpoint (state lẫn metadata)
    token = h["Authorization"].removeprefix("Bearer ")
    assert token not in str(snap.values) and token not in str(snap.metadata)