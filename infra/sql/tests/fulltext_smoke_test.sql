SET NOCOUNT ON;
USE ChatbotDb;
DELETE FROM dbo.KnowledgeChunks WHERE DocumentId >= 9000;

INSERT INTO dbo.KnowledgeChunks (DocumentId, ChunkIndex, Heading, Content) VALUES
 (9000, 0, N'Nghi phep nam', N'Nhân viên chính thức được nghỉ phép năm 12 ngày. Đơn nghỉ phép phải gửi trưởng phòng duyệt.'),
 (9001, 0, N'Dat phong hop', N'Phòng họp được đặt qua eOffice tối thiểu 1 ngày trước. Cuộc họp trên 20 người cần đặt hội trường.'),
 (9002, 0, N'Cap tai khoan', N'Nhân viên mới được cấp tài khoản email và eOffice trong ngày làm việc đầu tiên.');

WAITFOR DELAY '00:00:05';  -- chờ full-text index cập nhật

PRINT N'--- T1: CONTAINS có dấu (mong đợi: Nghi phep nam) ---';
SELECT Heading FROM dbo.KnowledgeChunks WHERE CONTAINS(Content, N'"nghỉ phép"');

PRINT N'--- T2: CONTAINS không dấu (mong đợi: Nghi phep nam) ---';
SELECT Heading FROM dbo.KnowledgeChunks WHERE CONTAINS(Content, N'"nghi phep"');

PRINT N'--- T3: FREETEXTTABLE xếp hạng (mong đợi: Dat phong hop đứng đầu) ---';
SELECT k.Heading, ft.[RANK]
FROM FREETEXTTABLE(dbo.KnowledgeChunks, Content, N'đặt phòng họp') AS ft
JOIN dbo.KnowledgeChunks AS k ON k.Id = ft.[KEY]
ORDER BY ft.[RANK] DESC;

DELETE FROM dbo.KnowledgeChunks WHERE DocumentId >= 9000;
PRINT N'Da xoa du lieu test.';