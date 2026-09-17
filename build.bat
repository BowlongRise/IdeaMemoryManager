@echo off
chcp 65001 >nul
title IDEA内存管理 构建脚本 - by BowlongRise

echo ========================================================
echo     ⚡ IDEA内存管理 构建工具 (BowlongRise)
echo ========================================================
echo.

echo [1/2] 正在编译 Release 配置...
dotnet build -c Release --no-restore
if %errorlevel% neq 0 (
    echo [ERROR] 编译失败，请检查上方诊断信息。
    pause
    exit /b %errorlevel%
)

echo.
echo [2/2] 正在同步产物到桌面...
powershell -NoProfile -Command "$WshShell = New-Object -ComObject WScript.Shell; $d = [Environment]::GetFolderPath('Desktop'); Copy-Item -Path 'bin\Release\net8.0-windows\IdeaMemoryManager.exe' -Destination (Join-Path $d 'IDEA内存管理.exe') -Force; $s = $WshShell.CreateShortcut((Join-Path $d 'IDEA内存管理.lnk')); $s.TargetPath = (Convert-Path 'bin\Release\net8.0-windows\IdeaMemoryManager.exe'); $s.WorkingDirectory = (Convert-Path 'bin\Release\net8.0-windows'); $s.IconLocation = (Convert-Path 'assets\app.ico'); $s.Save()" >nul 2>nul

echo.
echo 构建成功！
echo - 程序产物: bin\Release\net8.0-windows\IdeaMemoryManager.exe
echo - 桌面启动: %USERPROFILE%\Desktop\IDEA内存管理.exe
echo ========================================================
echo.
pause