@echo off
echo ===============================================
echo   WPF Music Player - AI Setup Script
echo ===============================================
echo.
echo This script will install the required Python dependencies
echo for the AI-powered audio separation feature.
echo.

REM Check if Python is installed
echo [1/3] Checking Python installation...
python --version 2>nul
if errorlevel 1 (
    echo ERROR: Python not found!
    echo.
    echo Please install Python 3.8 or later from:
    echo https://www.python.org/downloads/
    echo.
    echo Make sure to check "Add Python to PATH" during installation.
    echo.
    pause
    exit /b 1
)

echo Python found! Continuing...
echo.

REM Install audio-separator package
echo [2/3] Installing audio-separator package...
echo This may take a few minutes...
pip install audio-separator
if errorlevel 1 (
    echo ERROR: Failed to install audio-separator package.
    echo.
    echo Try running this command manually:
    echo pip install audio-separator
    echo.
    pause
    exit /b 1
)

echo.
echo [3/3] Testing installation...
python debug_audio_separator.py
if errorlevel 1 (
    echo WARNING: AI test failed, but dependencies are installed.
    echo The AI model will download automatically when first used.
) else (
    echo SUCCESS: AI is ready to use!
)

echo.
echo ===============================================
echo   Setup Complete!
echo ===============================================
echo.
echo You can now build and run the WPF Music Player.
echo The AI audio separation feature should work properly.
echo.
echo Note: The AI model (66MB) will download automatically
echo the first time you use the separation feature.
echo.
pause
