-- Script kiểm tra và sửa database
-- Chạy script này trong SSMS để đảm bảo tất cả các cột đã được tạo đúng

USE caro;
GO

PRINT '=== Kiểm tra các cột trong bảng AspNetUsers ===';
GO

-- Kiểm tra cột CreatedAt
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'CreatedAt')
    PRINT '✓ Cột CreatedAt đã tồn tại';
ELSE
BEGIN
    ALTER TABLE AspNetUsers ADD CreatedAt datetime2 NOT NULL DEFAULT GETDATE();
    PRINT '✓ Đã thêm cột CreatedAt';
END
GO

-- Kiểm tra cột LastActive
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LastActive')
    PRINT '✓ Cột LastActive đã tồn tại';
ELSE
BEGIN
    ALTER TABLE AspNetUsers ADD LastActive datetime2 NULL;
    PRINT '✓ Đã thêm cột LastActive';
END
GO

-- Kiểm tra cột Status
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'Status')
    PRINT '✓ Cột Status đã tồn tại';
ELSE
BEGIN
    ALTER TABLE AspNetUsers ADD Status nvarchar(max) NOT NULL DEFAULT 'offline';
    PRINT '✓ Đã thêm cột Status';
END
GO

-- Kiểm tra cột LastLoginTime
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LastLoginTime')
    PRINT '✓ Cột LastLoginTime đã tồn tại';
ELSE
BEGIN
    ALTER TABLE AspNetUsers ADD LastLoginTime datetime2 NULL;
    PRINT '✓ Đã thêm cột LastLoginTime';
END
GO

PRINT '';
PRINT '=== Kiểm tra các bảng đã được tạo ===';
GO

-- Kiểm tra bảng GameHistories
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'GameHistories')
    PRINT '✓ Bảng GameHistories đã tồn tại';
ELSE
    PRINT '✗ Bảng GameHistories chưa tồn tại';
GO

-- Kiểm tra bảng MoveRecords
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'MoveRecords')
BEGIN
    PRINT '✓ Bảng MoveRecords đã tồn tại';
    
    -- Kiểm tra foreign key
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_MoveRecords_GameHistories_GameHistoryId')
        PRINT '✓ Foreign key FK_MoveRecords_GameHistories_GameHistoryId đã tồn tại';
    ELSE
        PRINT '✗ Foreign key FK_MoveRecords_GameHistories_GameHistoryId chưa tồn tại';
    
    -- Kiểm tra xem có GameHistoryId1 không (không nên có)
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('MoveRecords') AND name = 'GameHistoryId1')
    BEGIN
        PRINT '⚠ Cột GameHistoryId1 không nên tồn tại, đang xóa...';
        -- Xóa foreign key trước nếu có
        IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_MoveRecords_GameHistories_GameHistoryId1')
        BEGIN
            ALTER TABLE MoveRecords DROP CONSTRAINT FK_MoveRecords_GameHistories_GameHistoryId1;
            PRINT '✓ Đã xóa foreign key FK_MoveRecords_GameHistories_GameHistoryId1';
        END
        -- Xóa cột
        ALTER TABLE MoveRecords DROP COLUMN GameHistoryId1;
        PRINT '✓ Đã xóa cột GameHistoryId1';
    END
    ELSE
        PRINT '✓ Không có cột GameHistoryId1 (đúng)';
END
ELSE
    PRINT '✗ Bảng MoveRecords chưa tồn tại';
GO

-- Kiểm tra bảng FriendRequests
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'FriendRequests')
    PRINT '✓ Bảng FriendRequests đã tồn tại';
ELSE
    PRINT '✗ Bảng FriendRequests chưa tồn tại';
GO

-- Kiểm tra bảng Friends
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Friends')
    PRINT '✓ Bảng Friends đã tồn tại';
ELSE
    PRINT '✗ Bảng Friends chưa tồn tại';
GO

-- Kiểm tra bảng BanRecords
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'BanRecords')
    PRINT '✓ Bảng BanRecords đã tồn tại';
ELSE
    PRINT '✗ Bảng BanRecords chưa tồn tại';
GO

PRINT '';
PRINT '=== Tóm tắt cấu trúc bảng AspNetUsers ===';
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

PRINT '';
PRINT 'Hoàn thành kiểm tra!';
GO

