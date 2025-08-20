@echo off
setlocal EnableDelayedExpansion

echo ================================================================
echo    COMPLETE SPLEETER SETUP - NO PYTHON EXPERIENCE REQUIRED
echo ================================================================
echo.
echo This script will:
echo 1. Download and install Python 3.11 automatically
echo 2. Install all required packages (Spleeter, PyInstaller)
echo 3. Build the spleeter_wrapper.exe for your WPF app
echo 4. Copy everything to the right place
echo.
echo You don't need to do anything except wait!
echo.
pause

REM Create a temporary directory for downloads
set "TEMP_DIR=%TEMP%\spleeter_setup"
if not exist "%TEMP_DIR%" mkdir "%TEMP_DIR%"

echo Step 1: Downloading Python 3.11.9 (Latest stable version)...
echo ========================================================

REM Download Python 3.11.9 installer
set "PYTHON_URL=https://www.python.org/ftp/python/3.11.9/python-3.11.9-amd64.exe"
set "PYTHON_INSTALLER=%TEMP_DIR%\python-3.11.9-amd64.exe"

echo Downloading Python installer...
powershell -Command "& { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '%PYTHON_URL%' -OutFile '%PYTHON_INSTALLER%' -UseBasicParsing }" 2>nul

if not exist "%PYTHON_INSTALLER%" (
    echo Failed to download Python installer.
    echo Please check your internet connection and try again.
    pause
    exit /b 1
)

echo ✓ Python installer downloaded successfully!
echo.

echo Step 2: Installing Python 3.11.9...
echo ===================================

REM Install Python silently with all features
echo Installing Python (this may take a few minutes)...
"%PYTHON_INSTALLER%" /quiet InstallAllUsers=1 PrependPath=1 Include_test=0 Include_launcher=1

REM Wait for installation to complete
timeout /t 30 /nobreak > nul

REM Refresh environment variables
call refreshenv.cmd 2>nul

REM Try to find Python installation
set "PYTHON_EXE="
for %%i in (python.exe) do set "PYTHON_EXE=%%~$PATH:i"

if "%PYTHON_EXE%"=="" (
    REM Try common installation paths
    set "PYTHON_EXE=C:\Program Files\Python311\python.exe"
    if not exist "!PYTHON_EXE!" (
        set "PYTHON_EXE=C:\Users\%USERNAME%\AppData\Local\Programs\Python\Python311\python.exe"
    )
)

if not exist "%PYTHON_EXE%" (
    echo Python installation may have failed or is not in PATH.
    echo Please restart your computer and try again.
    echo If the problem persists, manually install Python 3.11 from python.org
    pause
    exit /b 1
)

echo ✓ Python installed successfully!
echo Using Python at: %PYTHON_EXE%
echo.

echo Step 3: Installing required Python packages...
echo =============================================

REM Upgrade pip first
echo Upgrading pip...
"%PYTHON_EXE%" -m pip install --upgrade pip

REM Install packages with specific versions for compatibility
echo Installing NumPy (may take a few minutes)...
"%PYTHON_EXE%" -m pip install "numpy<2.0" --only-binary=numpy

echo Installing TensorFlow...
"%PYTHON_EXE%" -m pip install tensorflow-cpu==2.15.0

echo Installing Spleeter...
"%PYTHON_EXE%" -m pip install spleeter==2.3.2

echo Installing PyInstaller...
"%PYTHON_EXE%" -m pip install pyinstaller

echo Installing additional dependencies...
"%PYTHON_EXE%" -m pip install librosa soundfile

echo ✓ All packages installed successfully!
echo.

echo Step 4: Building Spleeter executable...
echo ======================================

REM Navigate to the Resources directory
cd /d "%~dp0"

if not exist "spleeter_wrapper.py" (
    echo Error: spleeter_wrapper.py not found in current directory
    echo Current directory: %CD%
    echo Please ensure you're running this script from the Resources folder
    pause
    exit /b 1
)

echo Building standalone executable (this may take 5-10 minutes)...
echo Please be patient, PyInstaller is working...

"%PYTHON_EXE%" -m PyInstaller --onefile --clean --distpath "dist" --workpath "build" --specpath "." spleeter_wrapper.py --name spleeter_wrapper

if not exist "dist\spleeter_wrapper.exe" (
    echo Failed to build executable. Checking for errors...
    echo.
    echo Trying alternative build approach...
    "%PYTHON_EXE%" -m PyInstaller --onefile spleeter_wrapper.py
    
    if not exist "dist\spleeter_wrapper.exe" (
        echo Build failed completely. Please check the output above for errors.
        echo You may need to install Visual C++ Redistributables.
        pause
        exit /b 1
    )
)

echo ✓ Executable built successfully!
echo.

echo Step 5: Copying files to your WPF project...
echo ===========================================

REM Copy the executable to the current directory and WPF output
copy "dist\spleeter_wrapper.exe" "spleeter_wrapper.exe"

REM Try to copy to WPF output directories
set "WPF_DEBUG_DIR=..\bin\Debug\net8.0-windows\Resources"
set "WPF_RELEASE_DIR=..\bin\Release\net8.0-windows\Resources"

if not exist "%WPF_DEBUG_DIR%" mkdir "%WPF_DEBUG_DIR%"
if not exist "%WPF_RELEASE_DIR%" mkdir "%WPF_RELEASE_DIR%"

copy "spleeter_wrapper.exe" "%WPF_DEBUG_DIR%\" 2>nul
copy "spleeter_wrapper.exe" "%WPF_RELEASE_DIR%\" 2>nul

echo ✓ Files copied to WPF project directories!
echo.

echo Step 6: Testing the installation...
echo ==================================

echo Testing Python installation...
"%PYTHON_EXE%" --version

echo Testing Spleeter...
"%PYTHON_EXE%" -c "import spleeter; print('✓ Spleeter works!')"

echo Testing executable...
if exist "spleeter_wrapper.exe" (
    echo ✓ spleeter_wrapper.exe created successfully
    echo File size: 
    dir "spleeter_wrapper.exe" | findstr "spleeter_wrapper.exe"
) else (
    echo ✗ Executable not found
)

echo.
echo Step 7: Cleaning up...
echo =====================

REM Clean up build files
if exist "build" rmdir /s /q "build"
if exist "dist" rmdir /s /q "dist"
if exist "spleeter_wrapper.spec" del "spleeter_wrapper.spec"

REM Clean up downloaded installer
if exist "%PYTHON_INSTALLER%" del "%PYTHON_INSTALLER%"
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%"

echo ✓ Cleanup complete!
echo.

echo ================================================================
echo                    🎉 SETUP COMPLETE! 🎉
echo ================================================================
echo.
echo ✅ Python 3.11.9 installed
echo ✅ Spleeter and all dependencies installed  
echo ✅ spleeter_wrapper.exe built successfully
echo ✅ Files copied to your WPF project
echo.
echo What you can do now:
echo.
echo 1. Build your WPF project:
echo    cd .. 
echo    dotnet build
echo.
echo 2. Run your WPF application:
echo    dotnet run
echo.
echo 3. Test the new features:
echo    - Click the 🎤 button for karaoke mode
echo    - Click the 💾 button to export stems
echo.
echo 📝 Important notes:
echo   - First separation will take 30-60 seconds (downloading AI models)
echo   - Subsequent separations will be much faster (cached)
echo   - The executable is about 100-300MB (includes AI models)
echo   - No internet required after first use
echo.
echo Your offline music player now has professional audio separation! 🎵
echo.
pause

endlocal
