-- Script để thêm các cột còn thiếu vào bảng AspNetUsers
-- Chạy script này trong SQL Server Management Studio nếu migrations không thể apply tự động

USE caro;
GO

-- Kiểm tra và thêm cột CreatedAt
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID('AspNetUsers') 
               AND name = 'CreatedAt')
BEGIN
    ALTER TABLE AspNetUsers 
    ADD CreatedAt datetime2 NOT NULL DEFAULT GETDATE();
    PRINT 'Đã thêm cột CreatedAt';
END
ELSE
BEGIN
    PRINT 'Cột CreatedAt đã tồn tại';
END
GO

-- Kiểm tra và thêm cột LastActive
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID('AspNetUsers') 
               AND name = 'LastActive')
BEGIN
    ALTER TABLE AspNetUsers 
    ADD LastActive datetime2 NULL;
    PRINT 'Đã thêm cột LastActive';
END
ELSE
BEGIN
    PRINT 'Cột LastActive đã tồn tại';
END
GO

-- Kiểm tra và thêm cột Status
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID('AspNetUsers') 
               AND name = 'Status')
BEGIN
    ALTER TABLE AspNetUsers 
    ADD Status nvarchar(max) NOT NULL DEFAULT 'offline';
    PRINT 'Đã thêm cột Status';
END
ELSE
BEGIN
    PRINT 'Cột Status đã tồn tại';
END
GO

-- Kiểm tra và thêm cột LastLoginTime (nếu chưa có)
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID('AspNetUsers') 
               AND name = 'LastLoginTime')
BEGIN
    ALTER TABLE AspNetUsers 
    ADD LastLoginTime datetime2 NULL;
    PRINT 'Đã thêm cột LastLoginTime';
END
ELSE
BEGIN
    PRINT 'Cột LastLoginTime đã tồn tại';
END
GO

-- Kiểm tra kết quả
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AspNetUsers'
    AND COLUMN_NAME IN ('CreatedAt', 'LastActive', 'Status', 'LastLoginTime')
ORDER BY COLUMN_NAME;
GO

PRINT 'Hoàn thành! Kiểm tra kết quả ở trên.';
GO

