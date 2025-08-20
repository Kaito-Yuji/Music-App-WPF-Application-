@echo off
echo ========================================
echo    QUICK FIX FOR EXISTING EXECUTABLE
echo ========================================
echo.

if not exist "spleeter_wrapper.exe" (
    echo No spleeter_wrapper.exe found. Please run a setup script first.
    pause
    exit /b 1
)

echo I see you have spleeter_wrapper.exe, but it was built with Python 3.13
echo which has dependency issues. Let me try a quick fix...
echo.

echo Step 1: Installing missing packages for Python 3.13
echo ==================================================

echo Trying to install compatible versions...

REM Try installing with relaxed version constraints
python -m pip install "numpy>=1.20,<2.0" --quiet
python -m pip install "tensorflow>=2.15" --quiet --no-deps
python -m pip install "spleeter>=2.1" --quiet --no-deps
python -m pip install librosa soundfile --quiet

echo.
echo Step 2: Testing the installation
echo ===============================

python -c "import numpy; print('✅ NumPy:', numpy.__version__)" 2>nul || echo "❌ NumPy failed"
python -c "import tensorflow; print('✅ TensorFlow:', tensorflow.__version__)" 2>nul || echo "❌ TensorFlow failed"
python -c "import spleeter; print('✅ Spleeter works!')" 2>nul || echo "❌ Spleeter failed"

echo.
echo Step 3: Rebuilding executable with current packages
echo =================================================

if exist "spleeter_wrapper.py" (
    echo Rebuilding with current Python setup...
    
    REM Clean previous build
    if exist "build" rmdir /s /q "build"
    if exist "dist" rmdir /s /q "dist"
    if exist "spleeter_wrapper.spec" del "spleeter_wrapper.spec"
    
    python -m PyInstaller --onefile spleeter_wrapper.py --hidden-import=spleeter --hidden-import=tensorflow
    
    if exist "dist\spleeter_wrapper.exe" (
        copy "dist\spleeter_wrapper.exe" "spleeter_wrapper_fixed.exe"
        echo ✅ New executable created: spleeter_wrapper_fixed.exe
        
        REM Copy to WPF project
        set "DEBUG_FOLDER=..\bin\Debug\net8.0-windows\Resources"
        mkdir "%DEBUG_FOLDER%" 2>nul
        copy "spleeter_wrapper_fixed.exe" "%DEBUG_FOLDER%\spleeter_wrapper.exe"
        
        echo ✅ Copied to WPF project
        
        REM Clean up
        rmdir /s /q "dist" 2>nul
        rmdir /s /q "build" 2>nul
        del "spleeter_wrapper.spec" 2>nul
        
    ) else (
        echo ❌ Rebuild failed
    )
) else (
    echo ❌ spleeter_wrapper.py not found
)

echo.
echo RESULT:
echo ======
if exist "spleeter_wrapper_fixed.exe" (
    echo ✅ Quick fix completed!
    echo ✅ New executable: spleeter_wrapper_fixed.exe
    echo ✅ Copied to WPF project
    echo.
    echo You can now try building and running your WPF app:
    echo   cd ..
    echo   dotnet build
    echo   dotnet run
    echo.
    echo If this still doesn't work, please run FIXED_SETUP.bat
    echo for a complete Python 3.11 installation.
) else (
    echo ❌ Quick fix failed.
    echo.
    echo Please run one of these for a proper setup:
    echo 1. FIXED_SETUP.bat (installs Python 3.11)
    echo 2. PORTABLE_SETUP.bat (portable approach)
)

echo.
pause
