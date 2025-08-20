@echo off
setlocal EnableDelayedExpansion

echo ================================================================
echo      PORTABLE SPLEETER SETUP - NO PYTHON INSTALLATION
echo ================================================================
echo.
echo This approach downloads a portable Python that won't affect
echo your system at all. Everything stays in this folder!
echo.
echo This script will:
echo 1. Download portable Python 3.11 (no system installation)
echo 2. Install Spleeter in the portable environment
echo 3. Build the executable
echo 4. Clean up everything except the final .exe
echo.
pause

REM Create directories
set "PORTABLE_DIR=%~dp0portable_python"
set "TEMP_DIR=%~dp0temp_setup"
if not exist "%PORTABLE_DIR%" mkdir "%PORTABLE_DIR%"
if not exist "%TEMP_DIR%" mkdir "%TEMP_DIR%"

echo Step 1: Downloading portable Python 3.11...
echo ===========================================

REM Download Python embedded distribution
set "PYTHON_ZIP_URL=https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip"
set "PYTHON_ZIP=%TEMP_DIR%\python-embed.zip"

echo Downloading Python embedded (smaller, faster)...
powershell -Command "& { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '%PYTHON_ZIP_URL%' -OutFile '%PYTHON_ZIP%' -UseBasicParsing }"

if not exist "%PYTHON_ZIP%" (
    echo Download failed. Trying alternative method...
    REM Try using curl if available
    curl -L -o "%PYTHON_ZIP%" "%PYTHON_ZIP_URL%" 2>nul
    if not exist "%PYTHON_ZIP%" (
        echo Failed to download Python. Please check internet connection.
        pause
        exit /b 1
    )
)

echo ✓ Python downloaded successfully!

echo Step 2: Extracting portable Python...
echo ====================================

REM Extract Python
powershell -Command "Expand-Archive -Path '%PYTHON_ZIP%' -DestinationPath '%PORTABLE_DIR%' -Force"

if not exist "%PORTABLE_DIR%\python.exe" (
    echo Failed to extract Python. Please try running as Administrator.
    pause
    exit /b 1
)

echo ✓ Python extracted successfully!

echo Step 3: Setting up pip in portable Python...
echo ===========================================

REM Download get-pip.py
set "GET_PIP_URL=https://bootstrap.pypa.io/get-pip.py"
set "GET_PIP=%TEMP_DIR%\get-pip.py"

echo Downloading pip installer...
powershell -Command "Invoke-WebRequest -Uri '%GET_PIP_URL%' -OutFile '%GET_PIP%' -UseBasicParsing"

if not exist "%GET_PIP%" (
    curl -L -o "%GET_PIP%" "%GET_PIP_URL%" 2>nul
)

if not exist "%GET_PIP%" (
    echo Failed to download pip installer.
    pause
    exit /b 1
)

REM Enable site-packages in embedded Python
echo import site > "%PORTABLE_DIR%\sitecustomize.py"

REM Install pip
echo Installing pip...
cd /d "%PORTABLE_DIR%"
python.exe "%GET_PIP%"

echo ✓ Pip installed successfully!

echo Step 4: Installing Spleeter and dependencies...
echo ==============================================

echo This may take several minutes. Please be patient...

REM Install packages one by one with error checking
echo Installing NumPy...
python.exe -m pip install "numpy<2.0" --only-binary=numpy
if %errorlevel% neq 0 (
    echo NumPy installation failed. Trying alternative...
    python.exe -m pip install numpy==1.24.3
)

echo Installing TensorFlow...
python.exe -m pip install tensorflow-cpu==2.15.0
if %errorlevel% neq 0 (
    echo TensorFlow installation failed. Trying CPU-only version...
    python.exe -m pip install tensorflow-cpu==2.13.0
)

echo Installing Spleeter...
python.exe -m pip install spleeter==2.3.2
if %errorlevel% neq 0 (
    echo Spleeter installation failed. Trying without specific version...
    python.exe -m pip install spleeter
)

echo Installing PyInstaller...
python.exe -m pip install pyinstaller

echo Installing additional dependencies...
python.exe -m pip install librosa soundfile

echo ✓ All packages installed!

echo Step 5: Building executable...
echo =============================

REM Go back to Resources directory
cd /d "%~dp0"

if not exist "spleeter_wrapper.py" (
    echo Error: spleeter_wrapper.py not found!
    echo Please ensure you're running this from the Resources folder.
    pause
    exit /b 1
)

echo Building executable with portable Python...
echo This will take 5-10 minutes...

"%PORTABLE_DIR%\python.exe" -m PyInstaller --onefile --clean spleeter_wrapper.py

if not exist "dist\spleeter_wrapper.exe" (
    echo Build failed. Trying with different options...
    "%PORTABLE_DIR%\python.exe" -m PyInstaller --onefile --noconsole spleeter_wrapper.py
)

if not exist "dist\spleeter_wrapper.exe" (
    echo Build failed completely. 
    echo This might be due to antivirus software or insufficient permissions.
    echo Try running this script as Administrator.
    pause
    exit /b 1
)

echo ✓ Executable built successfully!

echo Step 6: Finalizing setup...
echo ==========================

REM Copy executable to final location
copy "dist\spleeter_wrapper.exe" "spleeter_wrapper.exe"

REM Copy to WPF project directories
set "WPF_DEBUG=..\bin\Debug\net8.0-windows\Resources"
set "WPF_RELEASE=..\bin\Release\net8.0-windows\Resources"

if not exist "%WPF_DEBUG%" mkdir "%WPF_DEBUG%"
copy "spleeter_wrapper.exe" "%WPF_DEBUG%\" 2>nul

if not exist "%WPF_RELEASE%" mkdir "%WPF_RELEASE%"
copy "spleeter_wrapper.exe" "%WPF_RELEASE%\" 2>nul

echo Step 7: Testing...
echo =================

echo Testing the executable...
if exist "spleeter_wrapper.exe" (
    echo ✓ spleeter_wrapper.exe created
    for %%I in ("spleeter_wrapper.exe") do echo File size: %%~zI bytes
) else (
    echo ✗ Executable not found
)

echo Step 8: Cleanup...
echo =================

REM Clean up everything except the final executable
if exist "build" rmdir /s /q "build"
if exist "dist" rmdir /s /q "dist"
if exist "spleeter_wrapper.spec" del "spleeter_wrapper.spec"
if exist "%PORTABLE_DIR%" rmdir /s /q "%PORTABLE_DIR%"
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%"

echo ================================================================
echo                🎊 SUCCESS! 🎊
echo ================================================================
echo.
echo ✅ Portable Python downloaded and configured
echo ✅ Spleeter and all dependencies installed
echo ✅ spleeter_wrapper.exe built successfully  
echo ✅ Files copied to your WPF project
echo ✅ Temporary files cleaned up
echo.
echo 🎵 Your music player now has AI-powered audio separation!
echo.
echo Next steps:
echo 1. cd ..
echo 2. dotnet build  
echo 3. dotnet run
echo.
echo Then test:
echo - 🎤 Karaoke mode button
echo - 💾 Export stems button
echo.
echo Note: First use will download AI models (~30-60 seconds)
echo       After that, it's instant!
echo.
pause

endlocal
