from langchain_openai import ChatOpenAI
from app.config import OLLAMA_BASE_URL, OLLAMA_API_KEY, OLLAMA_MODEL

llm = ChatOpenAI(
    base_url=OLLAMA_BASE_URL,
    api_key=OLLAMA_API_KEY,
    model=OLLAMA_MODEL,
)

if __name__ == "__main__":
    response = llm.invoke("Xin chào, bạn là ai?")
    print(response.content)