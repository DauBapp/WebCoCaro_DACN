# Database Update Script for WebChoiCoCaro
# This script will apply migrations to the existing database

Write-Host "=== Database Migration Script ===" -ForegroundColor Green
Write-Host ""

# Check if dotnet is available
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK found: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET SDK not found. Please install .NET 8.0 SDK" -ForegroundColor Red
    exit 1
}

# Check if EF Core tools are installed
try {
    $efVersion = dotnet ef --version
    Write-Host "✅ EF Core tools found: $efVersion" -ForegroundColor Green
} catch {
    Write-Host "⚠️  EF Core tools not found. Installing..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}

Write-Host ""
Write-Host "📊 Database Information:" -ForegroundColor Cyan
Write-Host "   Server: MSI\SQLEXPRESS" -ForegroundColor White
Write-Host "   Database: caro" -ForegroundColor White
Write-Host "   Authentication: Windows Authentication" -ForegroundColor White
Write-Host ""

Write-Host "🔄 Applying migrations..." -ForegroundColor Yellow

try {
    # Apply migrations
    dotnet ef database update --project . --startup-project .
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ Database updated successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "🎉 Next steps:" -ForegroundColor Cyan
        Write-Host "   1. Start the application" -ForegroundColor White
        Write-Host "   2. Navigate to /Database" -ForegroundColor White
        Write-Host "   3. Click 'Kiểm tra kết nối'" -ForegroundColor White
        Write-Host "   4. Click 'Tạo tài khoản test'" -ForegroundColor White
        Write-Host "   5. Test login with: test@example.com / Test123!" -ForegroundColor White
    } else {
        Write-Host "❌ Failed to update database" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error applying migrations: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Alternative method:" -ForegroundColor Yellow
    Write-Host "   1. Start the application" -ForegroundColor White
    Write-Host "   2. Navigate to /Database" -ForegroundColor White
    Write-Host "   3. Use the web interface to update database" -ForegroundColor White
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 