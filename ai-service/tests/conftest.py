import os

# Tắt Langfuse khi chạy test: biến môi trường ưu tiên hơn .env
os.environ["LANGFUSE_PUBLIC_KEY"] = ""
os.environ["LANGFUSE_SECRET_KEY"] = ""