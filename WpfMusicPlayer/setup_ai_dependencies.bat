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

REM Install the Python packages both AI features need
echo [2/3] Installing Python packages...
echo   audio-separator + onnxruntime  - vocal separation
echo   librosa                        - mood detection
echo This may take several minutes...
pip install audio-separator onnxruntime librosa
if errorlevel 1 (
    echo ERROR: Failed to install the Python packages.
    echo.
    echo Try running this command manually:
    echo pip install audio-separator onnxruntime librosa
    echo.
    echo If pip cannot find a matching version, you are probably on a Python release
    echo that is too new. Python 3.12 is the version these packages support best.
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
echo Mood detection needs no model download - it measures the audio directly.
echo.
pause
