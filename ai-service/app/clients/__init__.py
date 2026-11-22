from app import config
from app.clients.types import ToolResult, NOT_FOUND_MSG
from app.clients.backend_client import BackendClient
from app.clients.mock_backend_client import MockBackendClient

_client = None

def get_backend_client():
    global _client
    if _client is None:
        _client = (MockBackendClient() if config.USE_MOCK
                   else BackendClient(config.BACKEND_URL, config.BACKEND_TIMEOUT))
    return _client

async def close_backend_client():
    global _client
    if _client is not None:
        await _client.aclose()
        _client = None