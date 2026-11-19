import json
import re
from typing import Literal, Optional, Dict, Any
from pydantic import BaseModel, Field
from langchain_core.messages import SystemMessage, HumanMessage, AIMessage
from langchain_core.output_parsers import JsonOutputParser

from app.llm import get_llm
from app.graph.state import NodeState
from app.services.api import api

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

    llm = get_llm(temperature=0.0)
    formatted_system = INTENT_SYSTEM_PROMPT.format(
        format_instructions=parser.get_format_instructions()
    )

    messages = [
        SystemMessage(content=formatted_system),
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

async def generate_answer_node(state: NodeState) -> Dict[str, Any]:
    question = state.get("question") or (
        state["messages"][-1].content if state.get("messages") else ""
    )
    intent = state.get("intent", "unknown")
    tool_result = state.get("tool_result")

    llm = get_llm(temperature=0.3)

    if intent in ["chitchat", "unknown"] or not tool_result:
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

def search_documents_node(state: NodeState) -> Dict[str, Any]:
    tool_args = state.get("tool_args", {})
    query = tool_args.get("query", state.get("question", ""))
    result = api.search_documents(query=query)
    return {"tool_result": result}

# 2. Get Status Node
def get_request_status_node(state: NodeState) -> Dict[str, Any]:
    tool_args = state.get("tool_args", {})
    req_id = tool_args.get("request_id")
    result = api.get_request_status(request_id=req_id)
    return {"tool_result": result}

# 3. Create Request Node
def create_request_node(state: NodeState) -> Dict[str, Any]:
    tool_args = state.get("tool_args", {})
    result = api.create_request(**tool_args)
    return {"tool_result": result}

# 4. Get Form Template Node
def get_form_template_node(state: NodeState) -> Dict[str, Any]:
    tool_args = state.get("tool_args", {})
    form_type = tool_args.get("form_type", "Leave")
    result = api.get_form_template(form_type=form_type)
    return {"tool_result": result}

# 5. List Requests Node
def list_my_requests_node(state: NodeState) -> Dict[str, Any]:
    tool_args = state.get("tool_args", {})
    result = api.list_my_requests(**tool_args)
    return {"tool_result": result}

# 6. Cancel Request Node
def cancel_request_node(state: NodeState) -> Dict[str, Any]:
    tool_args = state.get("tool_args", {})
    req_id = tool_args.get("request_id")
    reason = tool_args.get("cancel_reason")
    result = api.cancel_request(request_id=req_id, cancel_reason=reason)
    return {"tool_result": result}

# 7. Get Workflow Steps Node
def get_workflow_steps_node(state: NodeState) -> Dict[str, Any]:
    tool_args = state.get("tool_args", {})
    req_id = tool_args.get("request_id")
    result = api.get_workflow_steps(request_id=req_id)
    return {"tool_result": result}
