from dataclasses import dataclass
import jwt
from fastapi import Depends, HTTPException
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from app.config import settings


@dataclass(frozen=True)
class UserContext:
    user_id: int
    role: str
    dept_id: int | None
    token: str


def _unauthorized(message: str = "Phiên đăng nhập không hợp lệ."):
    raise HTTPException(status_code=401,
                        detail={"code": "UNAUTHORIZED", "message": message})


_bearer = HTTPBearer(auto_error=False)

async def get_current_user(
    credentials: HTTPAuthorizationCredentials | None = Depends(_bearer),
) -> UserContext:
    if credentials is None or credentials.scheme.lower() != "bearer":
        _unauthorized("Thiếu token.")
    token = credentials.credentials.strip()
    try:
        claims = jwt.decode(
            token, settings.jwt_key, algorithms=["HS256"],
            issuer=settings.jwt_issuer, audience=settings.jwt_audience,
            options={"require": ["exp", "sub", "iss", "aud"]},
        )
    except jwt.ExpiredSignatureError:
        _unauthorized("Token đã hết hạn.")
    except jwt.InvalidTokenError:
        _unauthorized()
    return UserContext(
        user_id=int(claims["sub"]),
        role=claims.get("role", ""),
        dept_id=claims.get("dept_id"),
        token=token,
    )