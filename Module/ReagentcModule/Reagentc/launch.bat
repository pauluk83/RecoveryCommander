@echo off
title ReAgentC GUI Launcher
cd /d "%~dp0"

echo ========================================================
echo         ReAgentC Windows RE Control Suite
echo ========================================================
echo.
echo Launching the native ReAgentC GUI...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0ReAgentC_GUI.ps1"
exit /b %errorlevel%
