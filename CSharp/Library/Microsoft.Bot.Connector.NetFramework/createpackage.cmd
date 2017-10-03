@echo off
setlocal
setlocal enabledelayedexpansion
setlocal enableextensions
set errorlevel=0
mkdir ..\nuget
erase /s ..\nuget\Microsoft.Bot.Connector*nupkg
rem msbuild /property:Configuration=release /p:DefineConstants="NET45" Microsoft.Bot.Connector.csproj 
dotnet restore ..\Microsoft.Bot.Connector.NetCore\Microsoft.Bot.Connector.NetCore.csproj 
dotnet msbuild /property:Configuration=release ..\Microsoft.Bot.Connector.NetCore\Microsoft.Bot.Connector.NetCore.csproj 
dotnet restore ..\Microsoft.Bot.Connector.AspNetCore\Microsoft.Bot.Connector.AspNetCore.csproj 
dotnet build --configuration Release ..\Microsoft.Bot.Connector.AspNetCore\Microsoft.Bot.Connector.AspNetCore.csproj --no-dependencies
for /f %%v in ('powershell -noprofile "(Get-Command .\bin\release\net45\Microsoft.Bot.Connector.dll).FileVersionInfo.FileVersion"') do set version=%%v
..\..\packages\NuGet.CommandLine.4.1.0\tools\NuGet.exe pack Microsoft.Bot.Connector.nuspec -symbols -properties version=%version% -OutputDirectory ..\nuget