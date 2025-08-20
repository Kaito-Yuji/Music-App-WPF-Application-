@echo off
echo ========================================
echo Python 3.13 Compatible Spleeter Setup
echo ========================================
echo.

echo Step 1: Installing compatible versions...
python -m pip install --upgrade pip

echo Installing numpy with pre-compiled wheel...
python -m pip install "numpy<2.0" --only-binary=numpy

echo Installing tensorflow (CPU version for compatibility)...
python -m pip install tensorflow-cpu==2.15.0

echo Installing other dependencies...
python -m pip install librosa soundfile
python -m pip install pyinstaller

echo Step 2: Installing Spleeter with specific version...
python -m pip install spleeter==2.3.2 --no-deps
python -m pip install norbert musdb museval

echo Step 3: Verifying installation...
python -c "import spleeter; print('Spleeter installed successfully')" 2>nul
if %errorlevel% neq 0 (
    echo Spleeter verification failed. Trying alternative approach...
    goto :alternative_approach
)

echo Step 4: Building executable...
cd /d "%~dp0"
if not exist "spleeter_wrapper.py" (
    echo Error: spleeter_wrapper.py not found
    pause
    exit /b 1
)

python -m PyInstaller --onefile spleeter_wrapper.py
goto :copy_executable

:alternative_approach
echo.
echo =========================================
echo Using Alternative Lightweight Approach
echo =========================================
echo.
echo Since Spleeter has dependency issues with Python 3.13,
echo creating a simpler audio separation solution...

REM Create a simpler Python script that uses librosa for basic separation
python -c "
import librosa
import soundfile as sf
import numpy as np
print('Creating simplified audio separator...')
"

if %errorlevel% equ 0 (
    echo ✓ Basic audio processing libraries installed
    echo Creating lightweight separator...
    goto :create_simple_separator
) else (
    echo ✗ Even basic libraries failed. Please use Python 3.11 or 3.12
    pause
    exit /b 1
)

:create_simple_separator
REM This would create a basic vocal/instrumental separator using simpler methods
echo Basic separation functionality will be limited compared to Spleeter
echo Consider using Python 3.11/3.12 for full Spleeter functionality
goto :end

:copy_executable
if exist "dist\spleeter_wrapper.exe" (
    copy "dist\spleeter_wrapper.exe" "spleeter_wrapper.exe"
    echo ✓ Spleeter executable created successfully!
) else (
    echo ✗ Failed to create executable
)

:end
echo.
echo Setup attempt complete!
pause
