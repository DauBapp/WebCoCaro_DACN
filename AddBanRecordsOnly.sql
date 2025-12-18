-- Script chỉ để thêm bảng BanRecords
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

PRINT 'BanRecords table added successfully!';
