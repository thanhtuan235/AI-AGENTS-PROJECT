"""A6 - Vector smoke test.
Nhúng chunk mẫu + câu hỏi bằng bge-m3 (Ollama), sinh file SQL để chạy bằng sqlcmd.
Dùng: python infra/sql/tests/vector_smoke_test.py
"""
import json
import os
import sys
import urllib.request
from pathlib import Path

OLLAMA_URL = os.getenv("OLLAMA_BASE_URL", "http://localhost:11434").rstrip("/")
MODEL = os.getenv("EMBEDDING_MODEL", "bge-m3")
DIM = 1024
TEST_DOC_BASE = 9000  # DocumentId >= 9000 là dữ liệu test, bị xóa trước và sau khi chạy

CHUNKS = [
    ("Nghi phep nam", "Nhân viên chính thức được nghỉ phép năm 12 ngày. Đơn nghỉ phép phải gửi trưởng phòng duyệt trước ít nhất 3 ngày làm việc."),
    ("Cong tac phi", "Khi đi công tác, nhân viên được thanh toán chi phí đi lại, lưu trú và phụ cấp theo quy định. Hồ sơ thanh toán nộp trong vòng 10 ngày sau chuyến đi."),
    ("Dat phong hop", "Phòng họp được đặt qua eOffice tối thiểu 1 ngày trước. Cuộc họp trên 20 người cần đặt hội trường tầng 5."),
    ("Cap tai khoan", "Nhân viên mới được cấp tài khoản email và eOffice trong ngày làm việc đầu tiên sau khi phòng Nhân sự gửi yêu cầu cho phòng CNTT."),
    ("Mua sam tai san", "Đề xuất mua sắm dưới 50 triệu đồng do giám đốc khối phê duyệt; từ 50 triệu đồng trở lên phải qua hội đồng mua sắm."),
]

# (nhãn, câu hỏi, heading mong đợi đứng đầu)
QUESTIONS = [
    ("Q1", "Tôi được nghỉ phép bao nhiêu ngày một năm?", "Nghi phep nam"),
    ("Q2", "Muốn đặt phòng họp cho 30 người thì làm sao?", "Dat phong hop"),
    ("Q3", "Mua laptop 60 triệu cần ai duyệt?", "Mua sam tai san"),
]


def embed(texts: list[str]) -> list[list[float]]:
    body = json.dumps({"model": MODEL, "input": texts}).encode("utf-8")
    req = urllib.request.Request(
        f"{OLLAMA_URL}/api/embed", data=body,
        headers={"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(req, timeout=180) as resp:
        vectors = json.load(resp)["embeddings"]
    for v in vectors:
        if len(v) != DIM:
            sys.exit(f"Sai số chiều: {len(v)} != {DIM}. Kiểm tra lại model {MODEL}.")
    return vectors


def vec_literal(v: list[float]) -> str:
    return "CAST('" + json.dumps(v, separators=(",", ":")) + f"' AS VECTOR({DIM}))"


def sql_str(s: str) -> str:
    return "N'" + s.replace("'", "''") + "'"


def main() -> None:
    chunk_vecs = embed([c for _, c in CHUNKS])
    q_vecs = embed([q for _, q, _ in QUESTIONS])

    lines = [
        "SET NOCOUNT ON;",
        "USE ChatbotDb;",
        f"DELETE FROM dbo.KnowledgeChunks WHERE DocumentId >= {TEST_DOC_BASE};",
    ]
    for i, ((heading, content), v) in enumerate(zip(CHUNKS, chunk_vecs)):
        lines.append(
            "INSERT INTO dbo.KnowledgeChunks (DocumentId, ChunkIndex, Heading, Content, Embedding) "
            f"VALUES ({TEST_DOC_BASE + i}, 0, {sql_str(heading)}, {sql_str(content)}, {vec_literal(v)});"
        )

    for n, ((label, _q, expected), v) in enumerate(zip(QUESTIONS, q_vecs), start=1):
        lines += [
            f"PRINT N'--- {label} (mong doi: {expected}) ---';",
            f"DECLARE @q{n} VECTOR({DIM}) = {vec_literal(v)};",
            "SELECT TOP 3 Heading, "
            f"CAST(VECTOR_DISTANCE('cosine', Embedding, @q{n}) AS DECIMAL(6,4)) AS Distance "
            "FROM dbo.KnowledgeChunks "
            f"WHERE IsActive = 1 AND DocumentId >= {TEST_DOC_BASE} AND Embedding IS NOT NULL "
            "ORDER BY Distance;",
            f"DECLARE @top{n} NVARCHAR(500) = (SELECT TOP 1 Heading FROM dbo.KnowledgeChunks "
            f"WHERE DocumentId >= {TEST_DOC_BASE} "
            f"ORDER BY VECTOR_DISTANCE('cosine', Embedding, @q{n}));",
            f"PRINT CASE WHEN @top{n} = {sql_str(expected)} THEN N'{label}: PASS' "
            f"ELSE N'{label}: FAIL (top1 = ' + @top{n} + N')' END;",
        ]

    lines.append(f"DELETE FROM dbo.KnowledgeChunks WHERE DocumentId >= {TEST_DOC_BASE};")
    lines.append("PRINT N'Da xoa du lieu test.';")

    out = Path(__file__).parent / "out" / "vector_smoke_test.generated.sql"
    out.parent.mkdir(exist_ok=True)
    out.write_text("\n".join(lines), encoding="utf-8")
    print(f"OK: {len(CHUNKS)} chunk, {len(QUESTIONS)} câu hỏi, vector {DIM} chiều -> {out}")


if __name__ == "__main__":
    main()