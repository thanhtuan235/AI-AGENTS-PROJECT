import json
import re
import unicodedata
from typing import Literal, Optional, Dict, Any
from pydantic import BaseModel, Field
from langchain_core.messages import SystemMessage, HumanMessage, AIMessage, BaseMessage
from langchain_core.output_parsers import JsonOutputParser
from langchain_core.runnables import RunnableConfig
from app.core.security import UserContext

from app.llm import get_llm
from app.graph.state import NodeState
from app import config
from app.clients import get_backend_client, ToolResult

# ==========================================
# Define output schema
# ==========================================

ToolNameType = Literal[
    "search_documents",    
    "get_request_status",  
    "create_request",       
    "get_form_template",   
    "list_my_requests",    
    "cancel_request",     
    "get_workflow_steps",  
    "chitchat",           
    "unknown"
]

class IntentToolCall(BaseModel):
    tool_name: ToolNameType = Field(
        description="Tên công cụ/hành động tương ứng khớp chính xác với Tool Contract"
    )
    # Args for get_request_status, cancel_request, get_workflow_steps
    request_id: Optional[int] = Field(
        default=None,
        description="Mã số ID của request (kiểu số nguyên int) nếu câu hỏi nhắc đến đơn cụ thể"
    )
    # Args for search_documents
    query: Optional[str] = Field(
        default=None,
        description="TỪ KHÓA TÌM KIẾM NGẮN GỌN (ví dụ: 'nghỉ phép', 'mua màn hình', 'công tác'), không truyền cả câu hỏi dài"
    )
    # Args for create_request / get_form_template
    request_type: Optional[Literal["Leave", "Purchase", "RoomBooking"]] = Field(
        default=None,
        description="Loại yêu cầu: 'Leave' (nghỉ phép), 'Purchase' (mua sắm), 'RoomBooking' (đặt phòng họp)"
    )
    start_date: Optional[str] = Field(
        default=None,
        description="Ngày bắt đầu (định dạng YYYY-MM-DD)"
    )
    end_date: Optional[str] = Field(
        default=None,
        description="Ngày kết thúc (định dạng YYYY-MM-DD)"
    )
    reason: Optional[str] = Field(
        default=None,
        description="Lý do tạo yêu cầu hoặc lý do hủy đơn"
    )
    extra_data: Optional[Dict[str, Any]] = Field(
        default=None,
        description="Dữ liệu bổ sung: Purchase: {item_name, quantity, estimated_cost}; RoomBooking: {room_id, time_slot, attendee_count}"
    )

parser = JsonOutputParser(pydantic_object=IntentToolCall)

# ==========================================
# Prompt guide for system 
# ==========================================

INTENT_SYSTEM_PROMPT = """Bạn là trợ lý AI điều phối (Router) cho hệ thống eOffice nội bộ.
Đọc yêu cầu của người dùng và trả về ĐÚNG 1 JSON OBJECT hợp lệ theo cấu trúc bên dưới để chọn Tool thích hợp. Tuyệt đối không thêm bất kỳ văn bản giải thích nào ngoài chuỗi JSON.

Quy tắc ánh xạ Tool:
1. "search_documents": Dùng khi tra cứu quy định, chính sách, hướng dẫn nội bộ. CHÚ Ý: Trường 'query' CHỈ chứa từ khóa ngắn gọn (ví dụ: "nghỉ phép", "chế độ thai sản"), không lấy cả câu hỏi.
2. "get_request_status": Tra cứu tình trạng/tiến độ của 1 đơn cụ thể đã có ID (ví dụ: "đơn số 3 duyệt chưa", "request 12 tới đâu rồi").
3. "create_request": Khi người dùng muốn tạo yêu cầu nghỉ phép, mua sắm vật tư, hoặc đặt phòng họp. Trích xuất loại ('Leave'/'Purchase'/'RoomBooking'), ngày tháng, lý do.
4. "get_form_template": Khi người dùng xin file mẫu đơn, biểu mẫu (Leave, Purchase, RoomBooking).
5. "list_my_requests": Khi người dùng hỏi tổng quan danh sách đơn của mình (ví dụ: "tôi có những yêu cầu nào", "danh sách đơn của tôi").
6. "cancel_request": Khi người dùng muốn hủy 1 đơn đang chờ duyệt.
7. "get_workflow_steps": Khi hỏi chi tiết ai đang duyệt, còn bao nhiêu bước duyệt của đơn.
8. "chitchat": Chào hỏi, cảm ơn, giao tiếp xã giao thông thường.
9. "unknown": Câu hỏi không rõ ràng, không liên quan đến công việc.

Ngữ cảnh hội thoại: các tin nhắn trước (nếu có) là lịch sử của cùng phiên. Nếu câu hỏi hiện tại
tham chiếu tới nội dung trước đó (ví dụ "đơn thứ hai", "đơn đó", "còn cái kia thì sao?"), hãy dùng
lịch sử để suy ra tool và request_id. Danh sách đơn trong lịch sử được đánh số "1.", "2.", ... kèm mã "#<id>".

Cấu trúc JSON yêu cầu:
{format_instructions}
"""

# ==========================================
# Helper parse json
# ==========================================

def _parse_llm_json(raw_text: str) -> Dict[str, Any]:
    raw_text = raw_text.strip()
    match = re.search(r"```(?:json)?\s*(\{.*?\})\s*```", raw_text, re.DOTALL)
    if match:
        raw_text = match.group(1)
    else:
        brace_match = re.search(r"(\{.*\})", raw_text, re.DOTALL)
        if brace_match:
            raw_text = brace_match.group(1)
    return json.loads(raw_text)

# ==========================================
# Intent detection node
# ==========================================

def _norm(text: str) -> str:
    """Chữ thường + bỏ dấu tiếng Việt, để bắt cả 'tao don' lẫn 'tạo đơn'."""
    text = unicodedata.normalize("NFD", text.lower())
    text = "".join(c for c in text if unicodedata.category(c) != "Mn")
    return text.replace("đ", "d")

_FORM_TYPES = {"nghi phep": "Leave", "mua sam": "Purchase", "mua": "Purchase",
               "dat phong": "RoomBooking", "phong hop": "RoomBooking"}

def _form_type(q: str) -> str:
    for kw, t in _FORM_TYPES.items():
        if kw in q:
            return t
    return "Leave"

def _rule_intent(question: str) -> Optional[Dict[str, Any]]:
    """Bắt các ý định không được phép để LLM đoán sai. Trả None nếu không khớp."""
    q = _norm(question)

    # 1. Hủy đơn
    if re.search(r"\b(huy|rut lai|thu hoi)\b.*\b(don|yeu cau|request)\b", q):
        return {"intent": "cancel_request", "tool_name": "cancel_request", "tool_args": {}}

    # 2. Xin biểu mẫu (kiểm trước "tạo" vì "mẫu đơn tạo..." vẫn là xin mẫu)
    if re.search(r"\b(mau don|bieu mau|form|file mau|mau)\b", q):
        return {"intent": "get_form_template", "tool_name": "get_form_template",
                "tool_args": {"form_type": _form_type(q)}}

    # 3. Tạo đơn
    if re.search(r"\b(tao|lam|viet|nop|gui|dang ky|xin)\b.*\b(don|yeu cau|request)\b", q) \
       or re.search(r"\bxin nghi\b", q):
        return {"intent": "create_request", "tool_name": "create_request", "tool_args": {}}

    return None

HISTORY_TURNS = 3   # số lượt gần nhất (user + assistant) đưa vào prompt


def _history(state: NodeState) -> list[BaseMessage]:
    """Lịch sử của phiên, BỎ tin hiện tại (luôn nằm cuối), chỉ giữ vài lượt gần nhất
    để prompt không phình theo độ dài phiên."""
    msgs = state.get("messages") or []
    return list(msgs[:-1][-HISTORY_TURNS * 2:])

async def detect_intent(state: NodeState) -> Dict[str, Any]:
    question = state.get("question") or (
        state["messages"][-1].content if state.get("messages") else ""
    )
    
    if not question:
        return {
            "intent": "unknown",
            "tool_name": None,
            "tool_args": {}
        }
    
    rule = _rule_intent(question)
    if rule is not None:
        return rule

    llm = get_llm(temperature=0.0)
    formatted_system = INTENT_SYSTEM_PROMPT.format(
        format_instructions=parser.get_format_instructions()
    )

    messages = [
        SystemMessage(content=formatted_system),
        *_history(state),
        HumanMessage(content=f"Yêu cầu: \"{question}\"")
    ]

    max_retries = 2
    parsed_data = {}

    for attempt in range(max_retries):
        try:
            response = await llm.ainvoke(messages)
            raw_content = response.content
            data = _parse_llm_json(raw_content)
            validated = IntentToolCall(**data)
            parsed_data = validated.model_dump()
            break
        except Exception:
            if attempt == max_retries - 1:
                parsed_data = {"tool_name": "unknown"}

    tool_name = parsed_data.get("tool_name", "unknown")
    tool_args: Dict[str, Any] = {}

    # Package `tool_args` in accordance with the parameter contract.
    if tool_name == "search_documents":
        tool_args = {
            "query": parsed_data.get("query") or question,
            "top_k": 5
        }
    elif tool_name in ["get_request_status", "get_workflow_steps"]:
        tool_args = {
            "request_id": parsed_data.get("request_id")
        }
    elif tool_name == "cancel_request":
        tool_args = {
            "request_id": parsed_data.get("request_id"),
            "cancel_reason": parsed_data.get("reason")
        }
    elif tool_name == "create_request":
        tool_args = {
            "request_type": parsed_data.get("request_type") or "Leave",
            "start_date": parsed_data.get("start_date"),
            "end_date": parsed_data.get("end_date"),
            "reason": parsed_data.get("reason"),
            "extra_data": parsed_data.get("extra_data")
        }
    elif tool_name == "get_form_template":
        tool_args = {
            "form_type": parsed_data.get("request_type") or "Leave"
        }
    elif tool_name == "list_my_requests":
        tool_args = {
            "status": "all",
            "request_type": "all",
            "limit": 20
        }

    return {
        "intent": tool_name,
        "tool_name": tool_name if tool_name not in ["unknown", "chitchat"] else None,
        "tool_args": tool_args
    }

# ==========================================
# Answer node
# ==========================================

ANSWER_SYSTEM_PROMPT = """Bạn là trợ lý AI chuyên nghiệp của hệ thống eOffice nội bộ công ty.
Nhiệm vụ của bạn là dựa vào câu hỏi của người dùng và dữ liệu (tool_result) được cung cấp để trả lời bằng tiếng Việt tự nhiên, ngắn gọn, lịch sự và rõ ràng.

Quy tắc trình bày:
- Trả lời đúng trọng tâm câu hỏi, định dạng Markdown (in đậm mã đơn, trạng thái, danh sách gạch đầu dòng) để dễ đọc.
- Tuyệt đối trung thực với dữ liệu từ tool_result, không tự bịa thông tin không có trong dữ liệu.
- Nếu tool_result báo lỗi hoặc không có dữ liệu, hãy thông báo lịch sự và hướng dẫn người dùng kiểm tra lại.
- Nếu là câu hỏi chào hỏi/xã giao (chitchat), hãy chào lại thân thiện và giới thiệu ngắn gọn các chức năng có thể hỗ trợ (tra cứu quy chế, xem trạng thái đơn, tạo yêu cầu nghỉ phép, mua sắm,...).
"""

_STATUS_VI = {"Pending": "Chờ phê duyệt", "Approved": "Đã phê duyệt",
              "Rejected": "Bị từ chối", "Cancelled": "Đã hủy", "Draft": "Nháp"}

def _template_answer(intent: str, data: Any) -> Optional[str]:
    if intent == "list_my_requests" and isinstance(data, list):
        if not data:
            return "Bạn chưa có đơn nào."
        lines = [f"Bạn có {len(data)} đơn:"]
        for i, r in enumerate(data, 1):
            status = _STATUS_VI.get(r.get("status"), r.get("status") or "Không rõ")
            lines.append(f"{i}. **#{r.get('id')}** – {r.get('title') or r.get('request_type')}"
                         f" – {status} ({str(r.get('created_at') or '')[:10]})")
        return "\n".join(lines)
    
    if not isinstance(data, dict):
        return None

    if intent == "get_form_template" and data.get("download_url"):
        return (f"Bạn có thể tải {data.get('title') or 'biểu mẫu'} tại đây: "
                f"[Tải biểu mẫu]({data['download_url']})")

    if intent in ("get_request_status", "get_workflow_steps") and data.get("id"):
        status = _STATUS_VI.get(data.get("status"), data.get("status") or "Không rõ")
        lines = [f"Đơn #{data['id']} – {data.get('title') or ''}".rstrip(" –"),
                 f"- Trạng thái: {status}"]
        if data.get("requester_name"):
            lines.append(f"- Người tạo: {data['requester_name']}")
        if data.get("created_at"):
            lines.append(f"- Ngày tạo: {str(data['created_at'])[:10]}")
        return "\n".join(lines)

    return None


async def generate_answer_node(state: NodeState) -> Dict[str, Any]:
    question = state.get("question") or (
        state["messages"][-1].content if state.get("messages") else ""
    )
    intent = state.get("intent", "unknown")
    tool_result = state.get("tool_result")

    # (a) Node trước đã có câu trả lời cố định (vd. write tool bị tắt) -> giữ nguyên
    if state.get("answer"):
        return {}

    # (b) Tool lỗi -> trả nguyên thông báo đã chuẩn hóa, không qua LLM
    if isinstance(tool_result, dict) and tool_result.get("success") is False:
        msg = (tool_result.get("error") or {}).get("message") \
              or "Hệ thống gặp lỗi, vui lòng thử lại sau."
        return {"answer": msg, "messages": [AIMessage(content=msg)]}

    # Tool thành công -> chỉ đưa phần data cho LLM, bỏ vỏ success/error
    if isinstance(tool_result, dict) and "data" in tool_result:
        tool_result = tool_result["data"]

    fixed = _template_answer(intent, tool_result)
    if fixed is not None:
        return {"answer": fixed, "messages": [AIMessage(content=fixed)]}

    llm = get_llm(temperature=0.3)

    if intent in ["chitchat", "unknown"] or tool_result is None:
        prompt_content = (
            f"Người dùng nói: \"{question}\"\n"
            f"Ý định nhận diện: {intent}.\n"
            f"Hãy phản hồi một cách tự nhiên, lịch sự và gợi mở hỗ trợ."
        )
    else:
        tool_data_str = json.dumps(tool_result, ensure_ascii=False, indent=2)
        prompt_content = f"""Câu hỏi của người dùng: "{question}"
                            Dữ liệu hệ thống trả về:
                            ```json
                            {tool_data_str}
                            Hãy dựa vào dữ liệu trên để tổng hợp câu trả lời tự nhiên, rõ ràng cho người dùng."""

    messages = [
        SystemMessage(content=ANSWER_SYSTEM_PROMPT),
        *_history(state),
        HumanMessage(content=prompt_content)
    ]

    response = await llm.ainvoke(messages)
    return {
        "answer": response.content,
        "messages": [AIMessage(content=response.content)] 
    }

# ==========================================
# Tool node
# ==========================================

WRITE_DISABLED_MSG = (
    "Chức năng tạo/hủy đơn qua chatbot chưa được hỗ trợ. "
    "Bạn vui lòng thao tác trực tiếp trên eOffice."
)

def _user(config: RunnableConfig) -> UserContext:
    """Lấy người dùng từ config["configurable"] (main.py gắn vào).
    Không bao giờ lấy từ state hay tool_args: LLM đọc/ghi được những chỗ đó."""
    user = (config.get("configurable") or {}).get("user")
    if user is None:
        raise RuntimeError("Thiếu 'user' trong config['configurable']")
    return user

def _token(config: RunnableConfig) -> str:
    return _user(config).token

def _result(r: ToolResult) -> Dict[str, Any]:
    """Đổi ToolResult -> dict để LangGraph/Langfuse serialize được."""
    return {"tool_result": r.model_dump()}

def _parse_id(value: Any) -> Optional[int]:
    """LLM có thể trả '101', 101, '#101' hoặc None -> đổi về int hoặc None."""
    if value is None:
        return None
    digits = re.sub(r"\D", "", str(value))
    return int(digits) if digits else None

async def search_documents_node(state: NodeState) -> Dict[str, Any]:
    # TODO(A6/Phase 1): RAG trên KnowledgeChunks. Phase 0 trả rỗng.
    return _result(ToolResult.ok([]))

# 2. Get Status Node
async def get_request_status_node(state: NodeState, config: RunnableConfig) -> Dict[str, Any]:
    args = state.get("tool_args") or {}
    req_id = _parse_id(args.get("request_id"))
    if req_id is None:
        return _result(ToolResult.fail(
            "VALIDATION_ERROR", "Bạn cho mình xin mã đơn cần tra cứu nhé."))
    r = await get_backend_client().get_request(_token(config), req_id)
    return _result(r)

# 3. Create Request Node
async def create_request_node(state: NodeState) -> Dict[str, Any]:
    if not config.ENABLE_WRITE_TOOLS:
        return {
            "answer": WRITE_DISABLED_MSG,
            "messages": [AIMessage(content=WRITE_DISABLED_MSG)],
        }
    raise NotImplementedError("create_request: làm ở Phase 1")

# 4. Get Form Template Node
async def get_form_template_node(state: NodeState, config: RunnableConfig) -> Dict[str, Any]:
    args = state.get("tool_args") or {}
    form_type = args.get("form_type") or "Leave"
    r = await get_backend_client().get_form(_token(config), form_type)
    return _result(r)

# 5. List Requests Node
async def list_my_requests_node(state: NodeState, config: RunnableConfig) -> Dict[str, Any]:
    args = state.get("tool_args") or {}
    r = await get_backend_client().list_my_requests(
        _token(config),
        status=args.get("status"),
        request_type=args.get("request_type"),
    )
    return _result(r)

# 6. Cancel Request Node
async def cancel_request_node(state: NodeState) -> Dict[str, Any]:
    if not config.ENABLE_WRITE_TOOLS:
        return {
            "answer": WRITE_DISABLED_MSG,
            "messages": [AIMessage(content=WRITE_DISABLED_MSG)],
        }
    raise NotImplementedError("cancel_request: làm ở Phase 1")

# 7. Get Workflow Steps Node
async def get_workflow_steps_node(state: NodeState, config: RunnableConfig) -> Dict[str, Any]:
    # Phase 0 chưa có GET /api/requests/{id}/workflow-steps -> tạm dùng chi tiết đơn
    return await get_request_status_node(config)
