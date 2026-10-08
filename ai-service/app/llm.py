from langchain_openai import ChatOpenAI
from app.config import OLLAMA_BASE_URL, OLLAMA_API_KEY, OLLAMA_MODEL

def get_llm(tempareture:float = 0.0) ->ChatOpenAI :
    llm = ChatOpenAI(
        base_url=OLLAMA_BASE_URL,
        api_key=OLLAMA_API_KEY,
        model=OLLAMA_MODEL,
        temperature=tempareture
    )

    return llm

if __name__ == "__main__":

    llm = get_llm(0.7);
    response = llm.invoke("Xin chào, bạn là ai?")
    print(response.content)