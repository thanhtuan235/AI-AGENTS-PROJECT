from typing import Annotated, Any, Dict, List, Optional
from typing_extensions import TypedDict
from langchain_core.messages import BaseMessage
from langgraph.graph.message import add_messages

class NodeState(TypedDict):
    question: str

    intent: Optional[str]

    tool_name: Optional[str]
    tool_args: Optional[Dict[str, Any]]
    tool_result: Optional[Any]

    answer: Optional[str]
    
    # add_messages: append message instead of overwrite
    messages: Annotated[List[BaseMessage], add_messages]
