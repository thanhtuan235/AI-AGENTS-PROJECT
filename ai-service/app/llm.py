from langchain_openai import ChatOpenAI
from app.config import OLLAMA_BASE_URL, OLLAMA_API_KEY, OLLAMA_MODEL

def get_llm(temperature:float = 0.0) ->ChatOpenAI :
    llm = ChatOpenAI(
        base_url=OLLAMA_BASE_URL,
        api_key=OLLAMA_API_KEY,
        model=OLLAMA_MODEL,
        temperature=temperature
    )
    return llm