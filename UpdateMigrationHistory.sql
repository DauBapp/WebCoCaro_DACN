-- Cập nhật migration history để đồng bộ với Entity Framework
-- Thêm migration history cho InitialCreate
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20241220000000_InitialCreate')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
    VALUES ('20241220000000_InitialCreate', '8.0.0');
END

-- Thêm migration history cho AddAdminFeatures
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20241220000000_AddAdminFeatures')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
    VALUES ('20241220000000_AddAdminFeatures', '8.0.0');
END

PRINT 'Migration history updated successfully!';
