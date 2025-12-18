-- Script để thêm các tính năng admin vào database hiện có
-- Thêm cột CreatedAt và LastLoginTime vào bảng AspNetUsers

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE();
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'LastLoginTime')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [LastLoginTime] datetime2 NULL;
END

-- Tạo bảng BanRecords
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BanRecords]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BanRecords](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [nvarchar](450) NOT NULL,
        [Reason] [nvarchar](500) NOT NULL,
        [BannedBy] [nvarchar](256) NOT NULL,
        [BannedAt] [datetime2](7) NOT NULL,
        [BanEndDate] [datetime2](7) NOT NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [UnbannedBy] [nvarchar](256) NULL,
        [UnbannedAt] [datetime2](7) NULL,
        CONSTRAINT [PK_BanRecords] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END

-- Thêm foreign key constraint cho BanRecords
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BanRecords_AspNetUsers_UserId]') AND parent_object_id = OBJECT_ID(N'[dbo].[BanRecords]'))
BEGIN
    ALTER TABLE [dbo].[BanRecords] WITH CHECK ADD CONSTRAINT [FK_BanRecords_AspNetUsers_UserId] 
    FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE;
END

-- Cập nhật dữ liệu cho các user hiện có (chỉ khi cột CreatedAt đã tồn tại)
-- Không cần thiết vì đã có DEFAULT GETDATE()

PRINT 'Admin features added successfully!';
