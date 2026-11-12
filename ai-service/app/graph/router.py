from app.graph.state import NodeState
from typing import Literal

# 1. "search_documents": Dùng khi tra cứu quy định, chính sách, hướng dẫn nội bộ. CHÚ Ý: Trường 'query' CHỈ chứa từ khóa ngắn gọn (ví dụ: "nghỉ phép", "chế độ thai sản"), không lấy cả câu hỏi.
# 2. "get_request_status": Tra cứu tình trạng/tiến độ của 1 đơn cụ thể đã có ID (ví dụ: "đơn số 3 duyệt chưa", "request 12 tới đâu rồi").
# 3. "create_request": Khi người dùng muốn tạo yêu cầu nghỉ phép, mua sắm vật tư, hoặc đặt phòng họp. Trích xuất loại ('Leave'/'Purchase'/'RoomBooking'), ngày tháng, lý do.
# 4. "get_form_template": Khi người dùng xin file mẫu đơn, biểu mẫu (Leave, Purchase, RoomBooking).
# 5. "list_my_requests": Khi người dùng hỏi tổng quan danh sách đơn của mình (ví dụ: "tôi có những yêu cầu nào", "danh sách đơn của tôi").
# 6. "cancel_request": Khi người dùng muốn hủy 1 đơn đang chờ duyệt.
# 7. "get_workflow_steps": Khi hỏi chi tiết ai đang duyệt, còn bao nhiêu bước duyệt của đơn.

def conditional_edge(state: NodeState) -> Literal["rag_node", "read_service_node", "write_action_node", "direct_response_node"]:
    intent = state['intent']

    if intent == "search_documents":
        return "rag_node"
    
    if intent in ["get_request_status", "get_form_template", "list_my_requests", "get_workflow_steps"]:
        return "read_service_node"
    
    if intent in ["create_request", "cancel_request"]:
        return "write_action_node"
        
    return "direct_response_node"
