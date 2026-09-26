from fastapi import FastAPI
from pydantic import BaseModel
from app.llm import llm

app = FastAPI(title="AI Service")


@app.get("/health")
def health():
    return {"status": "ok", "service": "ai-service"}


class ChatRequest(BaseModel):
    message: str


@app.post("/chat")
def chat(req: ChatRequest):
    response = llm.invoke(req.message)
    return {"response": response.content}