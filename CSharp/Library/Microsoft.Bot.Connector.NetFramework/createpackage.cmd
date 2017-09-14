@echo off
setlocal
setlocal enabledelayedexpansion
setlocal enableextensions
set errorlevel=0
mkdir ..\nuget
erase /s ..\nuget\Microsoft.Bot.Connector*nupkg
dotnet msbuild /property:Configuration=release Microsoft.Bot.Connector.csproj 
rem dotnet build --configuration Release ..\Microsoft.Bot.Connector.AspNetCore\project.json --no-dependencies
for /f %%v in ('powershell -noprofile "(Get-Command .\bin\release\Microsoft.Bot.Connector.dll).FileVersionInfo.FileVersion"') do set version=%%v
dotnet pack -c Release --no-build --include-source --include-symbols
