@echo off
REM Impeccable wrapper for iso worktree — does not modify product code.
set "LAB=C:\Users\User\OpenCode-UI-Lab-20261122-064208\node_modules\.bin\impeccable.cmd"
if exist "%LAB%" (
  call "%LAB%" %*
  exit /b %ERRORLEVEL%
)
where impeccable >nul 2>&1 && (
  impeccable %*
  exit /b %ERRORLEVEL%
)
echo Impeccable CLI not found. Install impeccable@^4.10.0 or restore the UI lab path. 1>&2
exit /b 127
