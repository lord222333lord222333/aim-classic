@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
if %ERRORLEVEL% NEQ 0 (
  echo Build failed!
  pause
  exit /b 1
)
echo Build succeeded: AIMClassic.exe and AIM_Setup.exe
