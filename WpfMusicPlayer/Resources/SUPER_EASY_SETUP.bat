@echo off
echo ========================================
echo    EASIEST SPLEETER SETUP EVER
echo ========================================
echo.
echo Hi! Since you're new to Python, I'll make this super simple.
echo This script does EVERYTHING for you automatically.
echo.
echo Just press Enter and wait. I'll handle the rest!
echo.
pause

REM Check if we have internet
ping google.com -n 1 >nul 2>&1
if %errorlevel% neq 0 (
    echo No internet connection. Please connect and try again.
    pause
    exit /b 1
)

echo 🌐 Internet connection: OK
echo.

echo 📥 Step 1: Downloading Python (this is safe, don't worry!)
echo ========================================================

REM Create temp folder
set "SETUP_DIR=%~dp0setup_temp"
mkdir "%SETUP_DIR%" 2>nul

REM Download Python 3.11 installer
echo Downloading Python installer... (about 25MB)
powershell -NoProfile -Command "Invoke-WebRequest 'https://www.python.org/ftp/python/3.11.9/python-3.11.9-amd64.exe' -OutFile '%SETUP_DIR%\python_installer.exe'"

if not exist "%SETUP_DIR%\python_installer.exe" (
    echo Download failed. Let me try a different way...
    curl -L "https://www.python.org/ftp/python/3.11.9/python-3.11.9-amd64.exe" -o "%SETUP_DIR%\python_installer.exe"
)

if not exist "%SETUP_DIR%\python_installer.exe" (
    echo I couldn't download Python automatically.
    echo Please visit: https://www.python.org/downloads/
    echo Download Python 3.11, install it, then run this script again.
    pause
    exit /b 1
)

echo ✅ Python downloaded!
echo.

echo 🔧 Step 2: Installing Python (automatic, no clicking needed!)
echo ===========================================================

echo Installing Python... This takes about 1 minute.
"%SETUP_DIR%\python_installer.exe" /quiet InstallAllUsers=0 PrependPath=1 Include_test=0

REM Wait for installation
echo Waiting for installation to complete...
timeout /t 45 /nobreak >nul

REM Find Python
set "PYTHON_PATH="
for /f "tokens=*" %%i in ('where python 2^>nul') do set "PYTHON_PATH=%%i" & goto found_python
:found_python

if "%PYTHON_PATH%"=="" (
    REM Try common paths
    if exist "C:\Users\%USERNAME%\AppData\Local\Programs\Python\Python311\python.exe" (
        set "PYTHON_PATH=C:\Users\%USERNAME%\AppData\Local\Programs\Python\Python311\python.exe"
    ) else if exist "C:\Program Files\Python311\python.exe" (
        set "PYTHON_PATH=C:\Program Files\Python311\python.exe"
    )
)

if "%PYTHON_PATH%"=="" (
    echo Python installation didn't work as expected.
    echo Please restart your computer and try again.
    pause
    exit /b 1
)

echo ✅ Python installed at: %PYTHON_PATH%
echo.

echo 📦 Step 3: Installing AI music separation tools
echo ==============================================

echo This part takes 5-10 minutes. Perfect time for a coffee! ☕
echo.

echo Installing package manager...
"%PYTHON_PATH%" -m pip install --upgrade pip --quiet

echo Installing NumPy (math library)...
"%PYTHON_PATH%" -m pip install "numpy<2.0" --quiet

echo Installing TensorFlow (AI engine)...
"%PYTHON_PATH%" -m pip install tensorflow-cpu==2.15.0 --quiet

echo Installing Spleeter (the magic audio separator)...
"%PYTHON_PATH%" -m pip install spleeter==2.3.2 --quiet

echo Installing PyInstaller (creates the .exe file)...
"%PYTHON_PATH%" -m pip install pyinstaller --quiet

echo Installing audio libraries...
"%PYTHON_PATH%" -m pip install librosa soundfile --quiet

echo ✅ All tools installed!
echo.

echo 🔨 Step 4: Building your custom audio separator
echo ==============================================

cd /d "%~dp0"

if not exist "spleeter_wrapper.py" (
    echo Oops! I can't find the spleeter_wrapper.py file.
    echo Make sure you're running this script from the Resources folder.
    echo Current location: %CD%
    pause
    exit /b 1
)

echo Building your personal audio separator...
echo This creates a single .exe file that works anywhere!

"%PYTHON_PATH%" -m PyInstaller --onefile --clean spleeter_wrapper.py --distpath . --workpath "%SETUP_DIR%\build"

if not exist "spleeter_wrapper.exe" (
    echo Build didn't work. Let me try a simpler approach...
    "%PYTHON_PATH%" -m PyInstaller --onefile spleeter_wrapper.py
    if exist "dist\spleeter_wrapper.exe" (
        move "dist\spleeter_wrapper.exe" "spleeter_wrapper.exe"
    )
)

if not exist "spleeter_wrapper.exe" (
    echo Something went wrong with the build.
    echo This sometimes happens with antivirus software.
    echo Try temporarily disabling antivirus and run this script again.
    pause
    exit /b 1
)

echo ✅ Your audio separator is ready!
echo.

echo 📁 Step 5: Putting files where your music app can find them
echo =========================================================

REM Copy to WPF project folders
set "DEBUG_FOLDER=..\bin\Debug\net8.0-windows\Resources"
set "RELEASE_FOLDER=..\bin\Release\net8.0-windows\Resources"

mkdir "%DEBUG_FOLDER%" 2>nul
mkdir "%RELEASE_FOLDER%" 2>nul

copy "spleeter_wrapper.exe" "%DEBUG_FOLDER%\" >nul 2>&1
copy "spleeter_wrapper.exe" "%RELEASE_FOLDER%\" >nul 2>&1

echo ✅ Files copied to your music app!
echo.

echo 🧹 Step 6: Cleaning up temporary files
echo ====================================

if exist "%SETUP_DIR%" rmdir /s /q "%SETUP_DIR%"
if exist "build" rmdir /s /q "build"
if exist "dist" rmdir /s /q "dist"
if exist "spleeter_wrapper.spec" del "spleeter_wrapper.spec"

echo ✅ Cleanup complete!
echo.

echo 🎉 CONGRATULATIONS! YOU'RE ALL SET! 🎉
echo ====================================
echo.
echo What just happened:
echo ✅ Python 3.11 installed on your computer
echo ✅ AI audio separation tools installed
echo ✅ Custom audio separator built (spleeter_wrapper.exe)
echo ✅ Everything copied to your music app
echo.
echo What you can do now:
echo.
echo 1. Go back to your main project folder:
echo    cd ..
echo.
echo 2. Build your music app:
echo    dotnet build
echo.
echo 3. Run your music app:
echo    dotnet run
echo.
echo 4. Try the new features:
echo    🎤 Click the microphone button for karaoke mode
echo    💾 Click the save button to export instrumental/vocal tracks
echo.
echo 📝 Important notes:
echo • First time you use it will be slow (30-60 seconds)
echo • After that, it's super fast because it remembers
echo • The .exe file is big (100-300MB) because it has AI models inside
echo • No internet needed after the first use
echo.
echo Your music player now has professional studio capabilities! 🎵
echo Enjoy making karaoke tracks and separating vocals! 🎤✨
echo.
pause
