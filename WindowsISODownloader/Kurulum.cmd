@echo off
setlocal
cd /d "%~dp0"
title Windows ISO Downloader - Kurulum
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Kurulum.ps1"
set "result=%errorlevel%"
if not "%result%"=="0" pause
exit /b %result%
