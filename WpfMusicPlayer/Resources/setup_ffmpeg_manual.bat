@echo off
echo Setting up portable FFmpeg for Spleeter...
echo.

REM Create ffmpeg directory
mkdir ffmpeg 2>nul

REM Check if ffmpeg.exe already exists
if exist "ffmpeg\ffmpeg.exe" (
    echo FFmpeg already installed.
    goto :test_setup
)

echo FFmpeg not found. Setting up minimal solution...
echo.

REM For now, let's create a test setup script
echo Creating setup instructions...

echo.
echo MANUAL SETUP REQUIRED:
echo 1. Go to: https://github.com/BtbN/FFmpeg-Builds/releases
echo 2. Download: ffmpeg-master-latest-win64-gpl.zip
echo 3. Extract ffmpeg.exe from bin folder to: %~dp0ffmpeg\
echo 4. Then run your WPF application again
echo.

REM Try to help the user by opening the download page
echo Opening download page...
start https://github.com/BtbN/FFmpeg-Builds/releases

:test_setup
echo.
echo Current setup:
if exist "ffmpeg\ffmpeg.exe" (
    echo [OK] FFmpeg found at: %~dp0ffmpeg\ffmpeg.exe
    ffmpeg\ffmpeg.exe -version | findstr "ffmpeg version"
) else (
    echo [MISSING] FFmpeg not found. Please download manually.
)

echo.
echo Testing Python script...
python spleeter_wrapper.py --help

echo.
pause
