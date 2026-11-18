/* =========================================================
   A6 - Vector schema cho ChatbotDb
   Chạy lại nhiều lần không lỗi (idempotent).
   Phần 1: KnowledgeChunks
   Phần 2: Full-text index      (thêm sau khi chốt phương án)
   Phần 3: Faqs.Embedding       (thêm sau khi BE xong B3)
   ========================================================= */

IF DB_ID(N'ChatbotDb') IS NULL
    CREATE DATABASE ChatbotDb;
GO

USE ChatbotDb;
GO

/* ---------- Phần 1: KnowledgeChunks ---------- */
IF OBJECT_ID(N'dbo.KnowledgeChunks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KnowledgeChunks (
        Id              INT IDENTITY(1,1) NOT NULL
                        CONSTRAINT PK_KnowledgeChunks PRIMARY KEY,
        DocumentId      INT            NOT NULL,
        DocumentVersion INT            NOT NULL CONSTRAINT DF_KC_DocumentVersion DEFAULT 1,
        ChunkIndex      INT            NOT NULL,
        Heading         NVARCHAR(500)  NULL,
        Content         NVARCHAR(MAX)  NOT NULL,
        Embedding       VECTOR(1024)   NULL,  -- bge-m3 = 1024 chiều
        AccessLevel     TINYINT        NOT NULL CONSTRAINT DF_KC_AccessLevel DEFAULT 0,
        DepartmentId    INT            NULL,  -- NULL = dùng chung toàn ngân hàng
        IsActive        BIT            NOT NULL CONSTRAINT DF_KC_IsActive DEFAULT 1,
        EffectiveDate   DATE           NULL,
        CreatedAt       DATETIME2(0)   NOT NULL CONSTRAINT DF_KC_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_KC_Doc_Version_Chunk UNIQUE (DocumentId, DocumentVersion, ChunkIndex)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_KC_Filter'
                 AND object_id = OBJECT_ID(N'dbo.KnowledgeChunks'))
    CREATE INDEX IX_KC_Filter
        ON dbo.KnowledgeChunks (IsActive, AccessLevel, DepartmentId);
GO

PRINT N'Phần 1 OK: KnowledgeChunks';
GO

/* ---------- Phần 2: Full-text index ---------- */
IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'ftKnowledge')
        CREATE FULLTEXT CATALOG ftKnowledge WITH ACCENT_SENSITIVITY = OFF;

    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes
                   WHERE object_id = OBJECT_ID(N'dbo.KnowledgeChunks'))
        CREATE FULLTEXT INDEX ON dbo.KnowledgeChunks
            (Heading LANGUAGE 0, Content LANGUAGE 0)
            KEY INDEX PK_KnowledgeChunks ON ftKnowledge
            WITH CHANGE_TRACKING AUTO;

    PRINT N'Phần 2 OK: Full-text index';
END
ELSE
    PRINT N'Phần 2 BỎ QUA: Full-Text Search chưa được cài';
GO