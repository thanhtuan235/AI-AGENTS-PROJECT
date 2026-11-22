from functools import lru_cache
from pathlib import Path
import os

from pydantic_settings import BaseSettings, SettingsConfigDict

# ai-service/app/config.py -> parents[2] = thư mục gốc repo (nơi đặt .env chung)
ROOT_ENV = Path(__file__).resolve().parents[2] / ".env"
LOCAL_ENV = Path(__file__).resolve().parents[1] / ".env"  # tùy chọn, để ghi đè riêng


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=(ROOT_ENV, LOCAL_ENV),  # file sau ghi đè file trước
        env_file_encoding="utf-8",
        extra="ignore",                  # .env chung có biến của .NET → bỏ qua
    )

    # Ollama
    ollama_base_url: str = "http://localhost:11434/v1"
    ollama_api_key: str = "ollama"
    ollama_model: str = "qwen2.5:3b"
    embedding_model: str = "bge-m3"
    embedding_dim: int = 1024

    # Langfuse
    langfuse_public_key: str | None = None
    langfuse_secret_key: str | None = None
    langfuse_host: str = "https://cloud.langfuse.com"

    # JWT (dùng ở A3)
    jwt_key: str = ""
    jwt_issuer: str = "eoffice-mock"
    jwt_audience: str = "eoffice-chatbot"

    # Backend (dùng ở A4)
    chatbot_backend_url: str = "http://localhost:5100"
    http_timeout: float = 10.0

    # Cờ
    use_mock: bool = True
    enable_write_tools: bool = False


@lru_cache
def get_settings() -> Settings:
    return Settings()

def _flag(name: str, default: str) -> bool:
    return os.getenv(name, default).lower() in ("1", "true", "yes")

CHATBOT_BACKEND_URL = os.getenv("CHATBOT_BACKEND_URL", "http://localhost:5100")   # Chatbot Backend
BACKEND_TIMEOUT = float(os.getenv("BACKEND_TIMEOUT", "10"))
USE_MOCK = _flag("USE_MOCK", "true")
ENABLE_WRITE_TOOLS = _flag("ENABLE_WRITE_TOOLS", "false")


settings = get_settings()