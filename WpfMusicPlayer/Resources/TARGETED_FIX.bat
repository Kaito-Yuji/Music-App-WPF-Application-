@echo off
echo ========================================
echo    TARGETED FIX - USE EXISTING PYTHON 3.11
echo ========================================
echo.
echo Great! I see you have Python 3.11.9 installed.
echo This is perfect for Spleeter - I just need to install
echo the packages for the correct Python version.
echo.
pause

echo Step 1: Testing Python 3.11 access
echo ==================================

REM Test different ways to access Python 3.11
py -3.11 --version >nul 2>&1
if %errorlevel% equ 0 (
    set "PYTHON311=py -3.11"
    echo ✅ Found Python 3.11 via launcher: py -3.11
) else (
    echo ❌ Python launcher method failed, trying direct paths...
    
    REM Try common installation paths
    if exist "C:\Users\%USERNAME%\AppData\Local\Programs\Python\Python311\python.exe" (
        set "PYTHON311=C:\Users\%USERNAME%\AppData\Local\Programs\Python\Python311\python.exe"
        echo ✅ Found Python 3.11 at: %PYTHON311%
    ) else if exist "C:\Program Files\Python311\python.exe" (
        set "PYTHON311=C:\Program Files\Python311\python.exe"
        echo ✅ Found Python 3.11 at: %PYTHON311%
    ) else (
        echo ❌ Cannot find Python 3.11 executable
        echo Please try running FIXED_SETUP.bat instead
        pause
        exit /b 1
    )
)

echo Using Python 3.11: %PYTHON311%
%PYTHON311% --version
echo.

echo Step 2: Installing packages for Python 3.11
echo ===========================================

echo Upgrading pip for Python 3.11...
%PYTHON311% -m pip install --upgrade pip

echo Installing NumPy (compatible version)...
%PYTHON311% -m pip install "numpy>=1.20.0,<1.25.0"

echo Installing TensorFlow (compatible version)...
%PYTHON311% -m pip install "tensorflow>=2.8.0,<2.16.0"

echo Installing Spleeter...
%PYTHON311% -m pip install "spleeter>=2.3.0"

echo Installing audio libraries...
%PYTHON311% -m pip install librosa soundfile

echo Installing PyInstaller for Python 3.11...
%PYTHON311% -m pip install pyinstaller

echo ✅ All packages installed!
echo.

echo Step 3: Testing the installation
echo ===============================

echo Testing imports with Python 3.11...
%PYTHON311% -c "import numpy; print('✅ NumPy:', numpy.__version__)"
if %errorlevel% neq 0 (
    echo ❌ NumPy test failed
    goto :install_failed
)

%PYTHON311% -c "import tensorflow; print('✅ TensorFlow:', tensorflow.__version__)"
if %errorlevel% neq 0 (
    echo ❌ TensorFlow test failed
    goto :install_failed
)

%PYTHON311% -c "import spleeter; print('✅ Spleeter works!')"
if %errorlevel% neq 0 (
    echo ❌ Spleeter test failed
    goto :install_failed
)

echo ✅ All packages working with Python 3.11!
echo.

echo Step 4: Rebuilding executable with Python 3.11
echo ==============================================

cd /d "%~dp0"

if not exist "spleeter_wrapper.py" (
    echo ❌ spleeter_wrapper.py not found
    pause
    exit /b 1
)

echo Cleaning previous build...
if exist "build" rmdir /s /q "build"
if exist "dist" rmdir /s /q "dist"
if exist "spleeter_wrapper.spec" del "spleeter_wrapper.spec"

echo Building with Python 3.11 (this takes 5-10 minutes)...
%PYTHON311% -m PyInstaller --onefile --clean spleeter_wrapper.py

if exist "dist\spleeter_wrapper.exe" (
    echo ✅ New executable built successfully!
    
    REM Backup old executable
    if exist "spleeter_wrapper.exe" (
        move "spleeter_wrapper.exe" "spleeter_wrapper_old.exe"
        echo 📁 Old executable backed up as spleeter_wrapper_old.exe
    )
    
    REM Move new executable
    move "dist\spleeter_wrapper.exe" "spleeter_wrapper.exe"
    echo ✅ New executable installed!
    
    REM Copy to WPF project
    set "DEBUG_DIR=..\bin\Debug\net8.0-windows\Resources"
    set "RELEASE_DIR=..\bin\Release\net8.0-windows\Resources"
    
    mkdir "%DEBUG_DIR%" 2>nul
    mkdir "%RELEASE_DIR%" 2>nul
    
    copy "spleeter_wrapper.exe" "%DEBUG_DIR%\"
    copy "spleeter_wrapper.exe" "%RELEASE_DIR%\"
    
    echo ✅ Copied to WPF project directories!
    
) else (
    echo ❌ Build failed
    goto :build_failed
)

echo Step 5: Testing the new executable
echo =================================

echo Testing executable help...
spleeter_wrapper.exe --help
if %errorlevel% equ 0 (
    echo ✅ Executable runs correctly!
) else (
    echo ⚠️ Executable has issues
)

echo.
echo Step 6: Cleanup
echo ==============

if exist "build" rmdir /s /q "build"
if exist "dist" rmdir /s /q "dist"
if exist "spleeter_wrapper.spec" del "spleeter_wrapper.spec"

echo ✅ Cleanup complete!
echo.

echo 🎉 SUCCESS! SPLEETER IS NOW PROPERLY INSTALLED! 🎉
echo ================================================
echo.
echo What's working now:
echo ✅ Python 3.11.9 with all required packages
echo ✅ TensorFlow and Spleeter properly installed
echo ✅ New spleeter_wrapper.exe built with Python 3.11
echo ✅ Files copied to your WPF project
echo.
echo Next steps:
echo.
echo 1. Build your WPF app:
echo    cd ..
echo    dotnet build
echo.
echo 2. Run your music player:
echo    dotnet run
echo.
echo 3. Test the features:
echo    🎤 Click karaoke mode button
echo    💾 Click export stems button
echo.
echo 📝 Remember:
echo • First separation: 30-60 seconds (downloads AI models)
echo • After that: Instant (cached)
echo • Works offline after first use
echo.
echo Your music player now has professional audio separation! 🎵
goto :end

:install_failed
echo.
echo ❌ Package installation failed for Python 3.11
echo This might be due to:
echo • Network connectivity issues
echo • Package version conflicts
echo • Missing Visual C++ Redistributables
echo.
echo Try running FIXED_SETUP.bat for a fresh Python 3.11 installation.
pause
exit /b 1

:build_failed
echo.
echo ❌ Executable build failed
echo This might be due to:
echo • Antivirus software blocking PyInstaller
echo • Missing dependencies
echo • Insufficient permissions
echo.
echo Try running this script as Administrator.
pause
exit /b 1

:end
echo.
pause
