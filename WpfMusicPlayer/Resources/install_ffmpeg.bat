@echo off
echo Installing FFmpeg for Spleeter...
echo.

REM Create ffmpeg directory
if not exist "ffmpeg" mkdir ffmpeg

REM Check if ffmpeg.exe already exists
if exist "ffmpeg\ffmpeg.exe" (
    echo FFmpeg already installed.
    goto :test_ffmpeg
)

echo Downloading FFmpeg...
echo Note: This is a manual step. Please download FFmpeg from:
echo https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip
echo.
echo Extract the contents to the "ffmpeg" folder in Resources directory.
echo Then run this script again.
echo.

REM Check if user has already downloaded it
if not exist "ffmpeg\ffmpeg.exe" (
    echo FFmpeg not found. Please download and extract it manually.
    pause
    exit /b 1
)

:test_ffmpeg
echo Testing FFmpeg...
ffmpeg\ffmpeg.exe -version | findstr /i "ffmpeg version"
if errorlevel 1 (
    echo FFmpeg test failed.
    pause
    exit /b 1
)

echo.
echo FFmpeg installed successfully!
echo Adding FFmpeg to PATH for current session...
set PATH=%~dp0ffmpeg;%PATH%

echo.
echo Testing Spleeter with FFmpeg...
python spleeter_wrapper.py --help

echo.
echo Setup complete! FFmpeg is ready for use with Spleeter.
pause
