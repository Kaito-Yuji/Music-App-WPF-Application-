@echo off
echo ================================================
echo Quick Setup - Download Pre-built Spleeter
echo ================================================
echo.

echo This approach downloads a pre-built Spleeter executable
echo to avoid Python dependency issues.
echo.

REM Check if we can download from the internet
ping -n 1 google.com >nul 2>&1
if %errorlevel% neq 0 (
    echo No internet connection detected.
    echo Please use the conda or manual setup approach.
    pause
    exit /b 1
)

echo Attempting to download pre-built executable...
echo.

REM Try to download using PowerShell
powershell -Command "& {
    try {
        Write-Host 'Downloading pre-built Spleeter executable...'
        # This would download from a releases page or build server
        # For now, we'll create a mock download
        Write-Host 'Note: Pre-built executable not available.'
        Write-Host 'Please use conda setup or Python 3.11/3.12'
        exit 1
    } catch {
        Write-Host 'Download failed.'
        exit 1
    }
}"

if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo   Alternative Setup Options
    echo ========================================
    echo.
    echo 1. Install Python 3.11 or 3.12 (Recommended)
    echo    Download from: https://www.python.org/downloads/
    echo    Then run: setup_spleeter.bat
    echo.
    echo 2. Use Conda (Easiest for dependency management)
    echo    Download Miniconda: https://docs.conda.io/en/latest/miniconda.html
    echo    Then run: setup_conda.bat
    echo.
    echo 3. Manual Docker approach (Advanced)
    echo    Use Docker container with Spleeter pre-installed
    echo.
    echo 4. Online API approach (Requires internet)
    echo    Use cloud-based audio separation service
    echo.
    pause
    exit /b 1
)

echo ✓ Setup complete!
pause
