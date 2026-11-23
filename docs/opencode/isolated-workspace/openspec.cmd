@echo off
REM OpenSpec wrapper for iso worktree — does not modify product code.
set "LAB=C:\Users\User\OpenCode-UI-Lab-20261122-064208\node_modules\.bin\openspec.cmd"
if exist "%LAB%" (
  call "%LAB%" %*
  exit /b %ERRORLEVEL%
)
where openspec >nul 2>&1 && (
  openspec %*
  exit /b %ERRORLEVEL%
)
echo OpenSpec CLI not found. Install @fission-ai/openspec@^2.1.1 or restore the UI lab path. 1>&2
exit /b 127
