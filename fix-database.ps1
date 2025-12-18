# Script tự động fix database - Thêm các cột còn thiếu
# Script này sẽ kết nối đến SQL Server và thêm các cột nếu chưa có

Write-Host "=== Script Fix Database ===" -ForegroundColor Green
Write-Host ""

$serverName = "MSI\SQLEXPRESS"
$databaseName = "caro"

Write-Host "Thông tin kết nối:" -ForegroundColor Cyan
Write-Host "  Server: $serverName" -ForegroundColor White
Write-Host "  Database: $databaseName" -ForegroundColor White
Write-Host ""

# Kiểm tra xem có file SQL không
$sqlFile = Join-Path $PSScriptRoot "FixDatabaseColumns.sql"
if (-not (Test-Path $sqlFile)) {
    Write-Host "❌ Không tìm thấy file FixDatabaseColumns.sql" -ForegroundColor Red
    Write-Host "Đang tạo script SQL trực tiếp..." -ForegroundColor Yellow
    
    $sqlScript = @"
USE caro;
GO

-- Thêm cột CreatedAt nếu chưa có
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE AspNetUsers ADD CreatedAt datetime2 NOT NULL DEFAULT GETDATE();
    PRINT 'Đã thêm cột CreatedAt';
END
GO

-- Thêm cột LastActive nếu chưa có
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LastActive')
BEGIN
    ALTER TABLE AspNetUsers ADD LastActive datetime2 NULL;
    PRINT 'Đã thêm cột LastActive';
END
GO

-- Thêm cột Status nếu chưa có
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'Status')
BEGIN
    ALTER TABLE AspNetUsers ADD Status nvarchar(max) NOT NULL DEFAULT 'offline';
    PRINT 'Đã thêm cột Status';
END
GO

-- Thêm cột LastLoginTime nếu chưa có
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LastLoginTime')
BEGIN
    ALTER TABLE AspNetUsers ADD LastLoginTime datetime2 NULL;
    PRINT 'Đã thêm cột LastLoginTime';
END
GO

PRINT 'Hoàn thành!';
GO
"@
    
    Write-Host "✓ Đã tạo script SQL" -ForegroundColor Green
    Write-Host ""
    Write-Host "Vui lòng:" -ForegroundColor Yellow
    Write-Host "1. Mở SQL Server Management Studio" -ForegroundColor White
    Write-Host "2. Kết nối đến $serverName" -ForegroundColor White
    Write-Host "3. Chọn database '$databaseName'" -ForegroundColor White
    Write-Host "4. Chạy script SQL sau:" -ForegroundColor White
    Write-Host ""
    Write-Host $sqlScript -ForegroundColor Gray
    Write-Host ""
    Write-Host "Hoặc copy script trên vào file FixDatabaseColumns.sql và chạy trong SSMS" -ForegroundColor Yellow
    exit 0
}

Write-Host "Tìm thấy file SQL script: $sqlFile" -ForegroundColor Green
Write-Host ""
Write-Host "⚠️  Vui lòng chạy script SQL trong SQL Server Management Studio:" -ForegroundColor Yellow
Write-Host "   1. Mở SSMS và kết nối đến: $serverName" -ForegroundColor White
Write-Host "   2. Chọn database: $databaseName" -ForegroundColor White
Write-Host "   3. Mở file: $sqlFile" -ForegroundColor White
Write-Host "   4. Chạy script (F5)" -ForegroundColor White
Write-Host ""
Write-Host "Hoặc chạy trực tiếp từ command line:" -ForegroundColor Cyan
Write-Host "   sqlcmd -S `"$serverName`" -d $databaseName -E -i `"$sqlFile`"" -ForegroundColor Gray
Write-Host ""

# Thử chạy sqlcmd nếu có
$sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue
if ($sqlcmdPath) {
    Write-Host "Đang thử chạy script SQL tự động..." -ForegroundColor Yellow
    try {
        $result = sqlcmd -S $serverName -d $databaseName -E -i $sqlFile -W
        Write-Host $result -ForegroundColor Green
        Write-Host ""
        Write-Host "✅ Script đã được chạy thành công!" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Không thể chạy script tự động: $_" -ForegroundColor Red
        Write-Host "Vui lòng chạy thủ công trong SSMS" -ForegroundColor Yellow
    }
}
else {
    Write-Host "Không tìm thấy sqlcmd. Vui lòng chạy script trong SSMS." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Sau khi chạy script SQL, hãy thử chạy lại ứng dụng:" -ForegroundColor Cyan
Write-Host "   dotnet run" -ForegroundColor White
Write-Host ""

