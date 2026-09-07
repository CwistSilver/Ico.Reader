@echo off

set /p VERSION=Enter package version:

REM Strip leading "v" if entered, e.g. v1.2.3 -> 1.2.3
if /i "%VERSION:~0,1%"=="v" set "VERSION=%VERSION:~1%"

REM pack builds, so -p:Version reaches the compiler and stamps every assembly in the package.
dotnet pack Ico.Reader/Ico.Reader.csproj -c Release -o ./artifacts -p:Version=%VERSION%
