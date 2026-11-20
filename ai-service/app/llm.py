from langchain_openai import ChatOpenAI

from app.config import settings


def get_llm(temperature: float = 0.0) -> ChatOpenAI:
    return ChatOpenAI(
        base_url=settings.ollama_base_url,
        api_key=settings.ollama_api_key,
        model=settings.ollama_model,
        temperature=temperature,
    )