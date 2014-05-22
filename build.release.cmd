@echo off

setlocal

set MSBUILD=C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
set PROJECT=%~dp0\src\ChessPlatform.sln

"%MSBUILD%" "%PROJECT%" /t:Rebuild /p:Configuration="Release" /p:Platform="Any CPU" || goto ERROR
goto :EOF

:ERROR
echo.
echo *** ERROR has occurred ***
pause
exit /b 1
