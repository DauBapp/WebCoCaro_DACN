-- =============================================
-- Script tạo Database hoàn chỉnh cho WebChoiCoCaro
-- Chạy script này trong SQL Server Management Studio
-- Kết nối đến: MSI\SQLEXPRESS
-- =============================================

USE master;
GO

-- Tạo database nếu chưa tồn tại
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'caro')
BEGIN
    CREATE DATABASE caro;
    PRINT '✓ Đã tạo database: caro';
END
ELSE
BEGIN
    PRINT '✓ Database caro đã tồn tại';
END
GO

USE caro;
GO

PRINT '';
PRINT '=== Bắt đầu tạo các bảng ===';
PRINT '';

-- =============================================
-- 1. TẠO CÁC BẢNG IDENTITY (ASP.NET Core Identity)
-- =============================================

-- Bảng AspNetRoles
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetRoles')
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
    PRINT '✓ Đã tạo bảng AspNetRoles';
END
ELSE
    PRINT '✓ Bảng AspNetRoles đã tồn tại';
GO

-- Bảng AspNetRoleClaims
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetRoleClaims')
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY(1,1),
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
    PRINT '✓ Đã tạo bảng AspNetRoleClaims';
END
ELSE
    PRINT '✓ Bảng AspNetRoleClaims đã tồn tại';
GO

-- Bảng AspNetUsers (với các cột custom)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUsers')
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        -- Các cột custom
        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
        [LastLoginTime] datetime2 NULL,
        [Status] nvarchar(max) NOT NULL DEFAULT 'offline',
        [LastActive] datetime2 NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
    CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
    PRINT '✓ Đã tạo bảng AspNetUsers với các cột custom';
END
ELSE
BEGIN
    PRINT '✓ Bảng AspNetUsers đã tồn tại';
    -- Thêm các cột custom nếu chưa có
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'CreatedAt')
    BEGIN
        ALTER TABLE [AspNetUsers] ADD [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE();
        PRINT '  ✓ Đã thêm cột CreatedAt';
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LastLoginTime')
    BEGIN
        ALTER TABLE [AspNetUsers] ADD [LastLoginTime] datetime2 NULL;
        PRINT '  ✓ Đã thêm cột LastLoginTime';
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'Status')
    BEGIN
        ALTER TABLE [AspNetUsers] ADD [Status] nvarchar(max) NOT NULL DEFAULT 'offline';
        PRINT '  ✓ Đã thêm cột Status';
    END
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LastActive')
    BEGIN
        ALTER TABLE [AspNetUsers] ADD [LastActive] datetime2 NULL;
        PRINT '  ✓ Đã thêm cột LastActive';
    END
END
GO

-- Bảng AspNetUserClaims
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUserClaims')
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
    PRINT '✓ Đã tạo bảng AspNetUserClaims';
END
ELSE
    PRINT '✓ Bảng AspNetUserClaims đã tồn tại';
GO

-- Bảng AspNetUserLogins
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUserLogins')
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
    PRINT '✓ Đã tạo bảng AspNetUserLogins';
END
ELSE
    PRINT '✓ Bảng AspNetUserLogins đã tồn tại';
GO

-- Bảng AspNetUserRoles
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUserRoles')
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
    PRINT '✓ Đã tạo bảng AspNetUserRoles';
END
ELSE
    PRINT '✓ Bảng AspNetUserRoles đã tồn tại';
GO

-- Bảng AspNetUserTokens
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUserTokens')
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    PRINT '✓ Đã tạo bảng AspNetUserTokens';
END
ELSE
    PRINT '✓ Bảng AspNetUserTokens đã tồn tại';
GO

-- =============================================
-- 2. TẠO CÁC BẢNG CUSTOM
-- =============================================

-- Bảng BanRecords
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BanRecords')
BEGIN
    CREATE TABLE [BanRecords] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UserId] nvarchar(450) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [BannedBy] nvarchar(256) NOT NULL,
        [BannedAt] datetime2 NOT NULL,
        [BanEndDate] datetime2 NOT NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [UnbannedBy] nvarchar(256) NULL,
        [UnbannedAt] datetime2 NULL,
        CONSTRAINT [PK_BanRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BanRecords_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_BanRecords_UserId] ON [BanRecords] ([UserId]);
    PRINT '✓ Đã tạo bảng BanRecords';
END
ELSE
    PRINT '✓ Bảng BanRecords đã tồn tại';
GO

-- Bảng GameHistories
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GameHistories')
BEGIN
    CREATE TABLE [GameHistories] (
        [Id] int NOT NULL IDENTITY(1,1),
        [RoomId] nvarchar(max) NOT NULL,
        [PlayerXId] nvarchar(max) NOT NULL,
        [PlayerOId] nvarchar(max) NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        [Winner] nvarchar(max) NULL,
        CONSTRAINT [PK_GameHistories] PRIMARY KEY ([Id])
    );
    PRINT '✓ Đã tạo bảng GameHistories';
END
ELSE
    PRINT '✓ Bảng GameHistories đã tồn tại';
GO

-- Bảng MoveRecords
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MoveRecords')
BEGIN
    CREATE TABLE [MoveRecords] (
        [Id] int NOT NULL IDENTITY(1,1),
        [GameHistoryId] int NOT NULL,
        [PlayerSymbol] nvarchar(max) NOT NULL,
        [Row] int NOT NULL,
        [Col] int NOT NULL,
        [MoveTime] datetime2 NOT NULL,
        CONSTRAINT [PK_MoveRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MoveRecords_GameHistories_GameHistoryId] FOREIGN KEY ([GameHistoryId]) REFERENCES [GameHistories] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_MoveRecords_GameHistoryId] ON [MoveRecords] ([GameHistoryId]);
    PRINT '✓ Đã tạo bảng MoveRecords';
END
ELSE
BEGIN
    PRINT '✓ Bảng MoveRecords đã tồn tại';
    -- Kiểm tra và xóa cột GameHistoryId1 nếu có (lỗi từ migration cũ)
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('MoveRecords') AND name = 'GameHistoryId1')
    BEGIN
        IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_MoveRecords_GameHistories_GameHistoryId1')
        BEGIN
            ALTER TABLE [MoveRecords] DROP CONSTRAINT [FK_MoveRecords_GameHistories_GameHistoryId1];
        END
        ALTER TABLE [MoveRecords] DROP COLUMN [GameHistoryId1];
        PRINT '  ✓ Đã xóa cột GameHistoryId1 không hợp lệ';
    END
END
GO

-- Bảng FriendRequests
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FriendRequests')
BEGIN
    CREATE TABLE [FriendRequests] (
        [Id] int NOT NULL IDENTITY(1,1),
        [SenderId] nvarchar(max) NOT NULL,
        [ReceiverId] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL DEFAULT 'Pending',
        [SentAt] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_FriendRequests] PRIMARY KEY ([Id])
    );
    PRINT '✓ Đã tạo bảng FriendRequests';
END
ELSE
    PRINT '✓ Bảng FriendRequests đã tồn tại';
GO

-- Bảng Friends
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Friends')
BEGIN
    CREATE TABLE [Friends] (
        [UserId] nvarchar(450) NOT NULL,
        [FriendId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Friends] PRIMARY KEY ([UserId], [FriendId]),
        CONSTRAINT [FK_Friends_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Friends_AspNetUsers_FriendId] FOREIGN KEY ([FriendId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
    PRINT '✓ Đã tạo bảng Friends';
END
ELSE
    PRINT '✓ Bảng Friends đã tồn tại';
GO

-- =============================================
-- 3. TẠO BẢNG MIGRATION HISTORY
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
    PRINT '✓ Đã tạo bảng __EFMigrationsHistory';
END
ELSE
    PRINT '✓ Bảng __EFMigrationsHistory đã tồn tại';
GO

-- Insert migration history
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20241220000000_InitialCreate')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
    VALUES ('20241220000000_InitialCreate', '8.0.17');
    PRINT '✓ Đã thêm migration history: InitialCreate';
END
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250815124536_BanRecordsOnly')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
    VALUES ('20250815124536_BanRecordsOnly', '8.0.17');
    PRINT '✓ Đã thêm migration history: BanRecordsOnly';
END
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251016063503_CreateGameHistoryAndMoveRecord')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
    VALUES ('20251016063503_CreateGameHistoryAndMoveRecord', '8.0.17');
    PRINT '✓ Đã thêm migration history: CreateGameHistoryAndMoveRecord';
END
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251026092035_AddFriendRequest')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
    VALUES ('20251026092035_AddFriendRequest', '8.0.17');
    PRINT '✓ Đã thêm migration history: AddFriendRequest';
END
GO

-- =============================================
-- 4. TẠO ROLE VÀ USER MẪU (OPTIONAL)
-- =============================================

PRINT '';
PRINT '=== Tạo Role và User mẫu ===';

-- Tạo Admin Role
IF NOT EXISTS (SELECT * FROM [AspNetRoles] WHERE [NormalizedName] = 'ADMIN')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());
    PRINT '✓ Đã tạo Role: Admin';
END
ELSE
    PRINT '✓ Role Admin đã tồn tại';
GO

PRINT '';
PRINT '=== HOÀN THÀNH ===';
PRINT '';
PRINT 'Database đã được tạo thành công!';
PRINT '';
PRINT 'Các bảng đã được tạo:';
PRINT '  - AspNetUsers (với CreatedAt, LastActive, Status, LastLoginTime)';
PRINT '  - AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetRoleClaims';
PRINT '  - AspNetUserLogins, AspNetUserTokens';
PRINT '  - BanRecords';
PRINT '  - GameHistories';
PRINT '  - MoveRecords';
PRINT '  - FriendRequests';
PRINT '  - Friends';
PRINT '';
PRINT 'Bạn có thể chạy ứng dụng ngay bây giờ!';
PRINT '  dotnet run';
PRINT '';
PRINT 'Hoặc đăng ký tài khoản mới tại: http://localhost:5178/Account/Register';
GO

