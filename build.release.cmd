@echo off

setlocal

set AppDir=%ProgramFiles%
if /i "%PROCESSOR_ARCHITECTURE%" equ "AMD64" set AppDir=%ProgramFiles(x86)%

set MSBUILD=%AppDir%\MSBuild\14.0\Bin\MSBuild.exe
set PROJECT=%~dp0\src\ChessPlatform.sln

"%MSBUILD%" "%PROJECT%" /t:Rebuild /p:Configuration="Release" /p:Platform="Any CPU" || goto ERROR
goto :EOF

:ERROR
echo.
echo *** ERROR has occurred ***
pause
exit /b 1
