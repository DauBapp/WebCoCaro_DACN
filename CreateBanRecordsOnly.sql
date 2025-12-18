-- Chỉ tạo bảng BanRecords, không thêm cột CreatedAt
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

-- Thêm foreign key
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BanRecords_AspNetUsers_UserId]'))
BEGIN
    ALTER TABLE [dbo].[BanRecords] 
    ADD CONSTRAINT [FK_BanRecords_AspNetUsers_UserId] 
    FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE;
END

-- Thêm migration history
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20241220000000_BanRecordsOnly')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
    VALUES ('20241220000000_BanRecordsOnly', '8.0.0');
END

PRINT 'BanRecords table created successfully!';