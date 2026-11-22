import httpx, pytest, respx
from app.clients.backend_client import BackendClient
from app.clients.types import NOT_FOUND_MSG

BASE, TOKEN = "http://backend.test", "tok-123"

@pytest.fixture
async def client():
    c = BackendClient(BASE)
    yield c
    await c.aclose()

@respx.mock
async def test_list_forward_token_va_boc_envelope(client):
    route = respx.get(f"{BASE}/api/requests").mock(return_value=httpx.Response(
        200, json={"success": True, "data": [{"id": 1}], "error": None}))
    r = await client.list_my_requests(TOKEN, status="Pending", request_type="all")
    assert r.success and r.data == [{"id": 1}]
    req = route.calls.last.request
    assert req.headers["Authorization"] == f"Bearer {TOKEN}"
    assert dict(req.url.params) == {"status": "Pending"}

@pytest.mark.parametrize("status", [403, 404])
@respx.mock
async def test_403_va_404_cung_thong_bao(client, status):
    respx.get(f"{BASE}/api/requests/9").mock(return_value=httpx.Response(status))
    r = await client.get_request(TOKEN, 9)
    assert r.error.code == "NOT_FOUND" and r.error.message == NOT_FOUND_MSG

@respx.mock
async def test_permission_denied_trong_envelope(client):
    respx.get(f"{BASE}/api/requests/9").mock(return_value=httpx.Response(200, json={
        "success": False, "data": None,
        "error": {"code": "PERMISSION_DENIED", "message": "x"}}))
    assert (await client.get_request(TOKEN, 9)).error.code == "NOT_FOUND"

@respx.mock
async def test_401(client):
    respx.get(f"{BASE}/api/forms/Leave").mock(return_value=httpx.Response(401))
    assert (await client.get_form(TOKEN, "Leave")).error.code == "UNAUTHORIZED"

@respx.mock
async def test_timeout(client):
    respx.get(f"{BASE}/api/forms/Leave").mock(side_effect=httpx.ReadTimeout("t"))
    assert (await client.get_form(TOKEN, "Leave")).error.code == "INTERNAL_ERROR"

@respx.mock
async def test_500_khong_lo_chi_tiet(client):
    respx.post(f"{BASE}/api/chat/messages").mock(
        return_value=httpx.Response(500, text="SqlException at line 42"))
    r = await client.save_message(TOKEN, 1, "user", "hi")
    assert r.error.code == "INTERNAL_ERROR" and "Sql" not in r.error.message

@respx.mock
async def test_create_session_va_save_message(client):
    respx.post(f"{BASE}/api/chat/sessions").mock(return_value=httpx.Response(
        200, json={"success": True, "data": {"session_id": 7}, "error": None}))
    respx.post(f"{BASE}/api/chat/messages").mock(return_value=httpx.Response(
        200, json={"success": True, "data": {"message_id": 99}, "error": None}))
    assert (await client.create_session(TOKEN)).data == {"session_id": 7}
    assert (await client.save_message(TOKEN, 7, "assistant", "ok")).data == {"message_id": 99}