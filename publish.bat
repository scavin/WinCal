@echo off
:: WinCal 一键发布脚本
:: 输出:
::   ./dist/framework-dependent/WinCal.exe  轻量版，需要安装 .NET 8 Desktop Runtime
::   ./dist/self-contained/WinCal.exe       独立版，无需安装运行时，体积较大

echo ========================================
echo   WinCal 发布脚本
echo ========================================
echo.

:: 清理旧的发布产物
if exist ".\dist" (
    echo [1/4] 清理旧的发布产物...
    rmdir /s /q ".\dist"
) else (
    echo [1/4] 无需清理
)

:: 发布
echo.
echo [2/4] 开始发布轻量版 (Release, win-x64, Framework-dependent, SingleFile)...
dotnet publish WinCal.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained false ^
  -p:PublishSingleFile=true ^
  -p:EnableCompressionInSingleFile=false ^
  -p:PublishReadyToRun=false ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  -o ./dist/framework-dependent

if %ERRORLEVEL% neq 0 (
    echo.
    echo [错误] 轻量版发布失败！请检查错误信息。
    pause
    exit /b 1
)

echo.
echo [3/4] 开始发布独立版 (Release, win-x64, Self-contained, SingleFile)...
dotnet publish WinCal.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:PublishReadyToRun=false ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  -o ./dist/self-contained

if %ERRORLEVEL% neq 0 (
    echo.
    echo [错误] 独立版发布失败！请检查错误信息。
    pause
    exit /b 1
)

:: 清理调试符号（可选）
echo.
echo [4/4] 清理调试符号...
if exist ".\dist\framework-dependent\WinCal.pdb" del ".\dist\framework-dependent\WinCal.pdb"
if exist ".\dist\self-contained\WinCal.pdb" del ".\dist\self-contained\WinCal.pdb"

:: 显示结果
echo.
echo ========================================
echo   发布完成！
echo ========================================
echo.
echo 输出目录: %cd%\dist\
echo.

:: 显示文件大小
for %%F in (".\dist\framework-dependent\WinCal.exe") do (
    set SMALL_SIZE=%%~zF
)
for %%F in (".\dist\self-contained\WinCal.exe") do (
    set STANDALONE_SIZE=%%~zF
)
echo 轻量版 WinCal.exe 大小: %SMALL_SIZE% 字节
echo 独立版 WinCal.exe 大小: %STANDALONE_SIZE% 字节
echo.
pause
