import time, jwt, pytest
from fastapi import FastAPI, Depends
from fastapi.testclient import TestClient
from app.config import settings
from app.core.security import get_current_user, UserContext

app = FastAPI()

@app.get("/me")
async def me(u: UserContext = Depends(get_current_user)):
    return {"user_id": u.user_id, "role": u.role}

client = TestClient(app)


def make_token(key=None, exp_offset=3600, **extra):
    payload = {"sub": "3", "role": "Employee", "dept_id": 1,
               "iss": "eoffice-mock", "aud": "eoffice-chatbot",
               "exp": int(time.time()) + exp_offset, **extra}
    return jwt.encode(payload, key or settings.jwt_key, algorithm="HS256")


def test_token_hop_le():
    r = client.get("/me", headers={"Authorization": f"Bearer {make_token()}"})
    assert r.status_code == 200 and r.json()["user_id"] == 3

def test_sai_chu_ky():
    t = make_token(key="khoa-sai-khoa-sai-khoa-sai-khoa-sai-123")
    assert client.get("/me", headers={"Authorization": f"Bearer {t}"}).status_code == 401

def test_het_han():
    t = make_token(exp_offset=-10)
    assert client.get("/me", headers={"Authorization": f"Bearer {t}"}).status_code == 401

def test_thieu_header():
    assert client.get("/me").status_code == 401