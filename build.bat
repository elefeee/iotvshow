@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo ==== 0. 加载 VS2022 构建环境 ====
set "VSWHERE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2^>nul`) do (
    set "MSBUILD=%%i"
)

if not defined MSBUILD (
    echo [找不到 MSBuild，尝试加载 VS Developer 环境]
    call :loadEnv
    goto :build
)

echo 使用 MSBuild: %MSBUILD%
goto :build

:loadEnv
rem 尝试直接调用 VS2022 的开发者命令提示环境脚本
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat" (
    call "%ProgramFiles%\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat" >nul 2>&1
) else if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat" (
    call "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat" >nul 2>&1
)
goto :eof

:build
echo ==== 1. 杀掉已运行的 IOTV 进程 ====
taskkill /F /IM IOTV.exe >nul 2>&1
timeout /t 1 /nobreak >nul

echo ==== 2. NuGet 还原 ====
dotnet restore src\iotvshow\iotvshow.csproj -r win-x64
if errorlevel 1 ( echo [还原失败] & pause & exit /b 1 )

echo ==== 3. 编译 ====
if defined MSBUILD (
    "%MSBUILD%" src\iotvshow\iotvshow.csproj /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64
) else (
    msbuild src\iotvshow\iotvshow.csproj /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64
)
if errorlevel 1 ( echo [编译失败] & pause & exit /b 1 )

echo ==== 4. 拷贝 res 下全部文件(含子目录) ====
set "OUT=src\iotvshow\bin\x64\Release\net472\win-x64"
if not exist "%OUT%\" md "%OUT%\"
:: /E 复制所有子目录包括空目录，/Y 覆盖不提示
xcopy "res\*" "%OUT%\" /E /Y >nul 2>&1

echo ==== 完成 ====
echo 产物目录: %cd%\%OUT%
pause