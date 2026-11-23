from typing import Literal
from langgraph.graph import StateGraph, START, END
from langgraph.checkpoint.memory import MemorySaver
from langgraph.types import Checkpointer

from app.graph.state import NodeState
from app.graph.nodes import (
    detect_intent,
    generate_answer_node,
    search_documents_node,
    get_request_status_node,
    create_request_node,
    get_form_template_node,
    list_my_requests_node,
    cancel_request_node,
    get_workflow_steps_node,
)

# ==========================================
# Router Function (Conditional Edge)
# ==========================================

RouteTarget = Literal[
    "search_documents_node",
    "get_request_status_node",
    "create_request_node",
    "get_form_template_node",
    "list_my_requests_node",
    "cancel_request_node",
    "get_workflow_steps_node",
    "generate_answer_node",
]

def route_intent(state: NodeState) -> RouteTarget:
    intent = state.get("intent", "unknown")

    tool_mapping = {
        "search_documents": "search_documents_node",
        "get_request_status": "get_request_status_node",
        "create_request": "create_request_node",
        "get_form_template": "get_form_template_node",
        "list_my_requests": "list_my_requests_node",
        "cancel_request": "cancel_request_node",
        "get_workflow_steps": "get_workflow_steps_node",
    }
    
    return tool_mapping.get(intent, "generate_answer_node")

# ==========================================
# 2. Build & Compile StateGraph
# ==========================================

def build_graph(checkpointer: Checkpointer = None):
    builder = StateGraph(NodeState)

    builder.add_node("detect_intent", detect_intent)
    builder.add_node("generate_answer_node", generate_answer_node)

    tool_nodes = [
        ("search_documents_node", search_documents_node),
        ("get_request_status_node", get_request_status_node),
        ("create_request_node", create_request_node),
        ("get_form_template_node", get_form_template_node),
        ("list_my_requests_node", list_my_requests_node),
        ("cancel_request_node", cancel_request_node),
        ("get_workflow_steps_node", get_workflow_steps_node),
    ]

    for node_name, node_func in tool_nodes:
        builder.add_node(node_name, node_func)

    builder.add_edge(START, "detect_intent")

    builder.add_conditional_edges(
        "detect_intent",
        route_intent,
        {
            "search_documents_node": "search_documents_node",
            "get_request_status_node": "get_request_status_node",
            "create_request_node": "create_request_node",
            "get_form_template_node": "get_form_template_node",
            "list_my_requests_node": "list_my_requests_node",
            "cancel_request_node": "cancel_request_node",
            "get_workflow_steps_node": "get_workflow_steps_node",
            "generate_answer_node": "generate_answer_node",
        }
    )

    for node_name, _ in tool_nodes:
        builder.add_edge(node_name, "generate_answer_node")

    builder.add_edge("generate_answer_node", END)

    return builder.compile(checkpointer=checkpointer)

checkpointer = MemorySaver()
graph = build_graph(checkpointer)