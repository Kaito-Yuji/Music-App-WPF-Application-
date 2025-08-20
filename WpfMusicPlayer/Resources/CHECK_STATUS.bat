@echo off
echo ========================================
echo    CHECKING CURRENT SPLEETER STATUS
echo ========================================
echo.

echo Let me check what we currently have installed...
echo.

echo Python versions on your system:
echo ================================
python --version 2>nul
py -3.13 --version 2>nul
py -3.11 --version 2>nul
py -3.10 --version 2>nul

echo.
echo Checking if spleeter_wrapper.exe was created:
echo ==========================================
if exist "spleeter_wrapper.exe" (
    echo ✅ spleeter_wrapper.exe EXISTS
    for %%I in ("spleeter_wrapper.exe") do echo File size: %%~zI bytes
    echo.
    echo Testing the executable:
    echo =====================
    spleeter_wrapper.exe --help 2>nul
    if !errorlevel! equ 0 (
        echo ✅ Executable runs without errors
    ) else (
        echo ⚠️  Executable has issues (missing dependencies)
    )
) else (
    echo ❌ spleeter_wrapper.exe NOT FOUND
)

echo.
echo Checking Python packages:
echo ========================
echo Testing with Python 3.13:
python -c "import spleeter; print('✅ Spleeter works with Python 3.13')" 2>nul || echo "❌ Spleeter not working with Python 3.13"
python -c "import tensorflow; print('✅ TensorFlow works with Python 3.13')" 2>nul || echo "❌ TensorFlow not working with Python 3.13"

echo.
echo Testing with py -3.11 (if available):
py -3.11 -c "import spleeter; print('✅ Spleeter works with Python 3.11')" 2>nul || echo "❌ Spleeter not working with Python 3.11 (or Python 3.11 not installed)"

echo.
echo RECOMMENDATION:
echo ==============
if exist "spleeter_wrapper.exe" (
    echo The executable exists but may not work properly due to missing dependencies.
    echo.
    echo Options:
    echo 1. Run FIXED_SETUP.bat to install Python 3.11 and rebuild properly
    echo 2. Test the current executable in your WPF app (it might work for basic functions)
    echo 3. Try the current executable with a simple audio file first
) else (
    echo No executable found. You need to run one of the setup scripts:
    echo 1. FIXED_SETUP.bat (recommended - installs Python 3.11)
    echo 2. PORTABLE_SETUP.bat (alternative - portable Python)
)

echo.
pause
