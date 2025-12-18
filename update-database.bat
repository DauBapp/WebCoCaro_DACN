@echo off
echo === Database Migration Script ===
echo.

REM Check if PowerShell is available
powershell -Command "Get-Host" >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ PowerShell not found
    pause
    exit /b 1
)

echo ✅ PowerShell found
echo.

REM Run the PowerShell script
powershell -ExecutionPolicy Bypass -File "%~dp0update-database.ps1"

echo.
echo Press any key to exit...
pause >nul 