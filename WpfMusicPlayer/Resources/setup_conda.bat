@echo off
echo ========================================
echo Spleeter Setup with Conda (Recommended)
echo ========================================
echo.

REM Check if conda is available
conda --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Conda not found. Installing Miniconda...
    echo.
    echo Please download and install Miniconda from:
    echo https://docs.conda.io/en/latest/miniconda.html
    echo.
    echo After installation, restart this script.
    pause
    exit /b 1
)

echo Step 1: Creating conda environment for Spleeter...
conda create -n spleeter python=3.11 -y
if %errorlevel% neq 0 (
    echo Failed to create conda environment
    pause
    exit /b 1
)

echo Step 2: Activating environment and installing packages...
call conda activate spleeter
conda install -c conda-forge numpy scipy tensorflow -y
pip install spleeter pyinstaller

echo Step 3: Building Spleeter executable...
cd /d "%~dp0"
if not exist "spleeter_wrapper.py" (
    echo Error: spleeter_wrapper.py not found
    pause
    exit /b 1
)

python -m PyInstaller --onefile --clean --distpath "dist" --workpath "build" --specpath "." spleeter_wrapper.py --name spleeter_wrapper

echo Step 4: Copying executable to WPF project...
if exist "dist\spleeter_wrapper.exe" (
    copy "dist\spleeter_wrapper.exe" "spleeter_wrapper.exe"
    echo ✓ Spleeter executable created successfully!
    echo.
    echo The executable 'spleeter_wrapper.exe' has been created.
    echo Make sure to include this file when distributing your WPF application.
    echo.
    echo You can now deactivate the conda environment:
    echo   conda deactivate
    echo.
    echo And optionally remove it if you don't need it anymore:
    echo   conda env remove -n spleeter
    echo.
) else (
    echo ✗ Failed to create spleeter_wrapper.exe
    echo Check the build output above for errors.
)

echo Step 5: Cleaning up build files...
if exist "build" rmdir /s /q "build"
if exist "spleeter_wrapper.spec" del "spleeter_wrapper.spec"
if exist "dist" rmdir /s /q "dist"

echo.
echo Setup complete!
call conda deactivate
pause
