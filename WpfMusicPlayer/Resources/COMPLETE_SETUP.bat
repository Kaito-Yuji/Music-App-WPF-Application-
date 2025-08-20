@echo off
echo =========================================================
echo    Spleeter Integration - Final Setup Steps
echo =========================================================
echo.
echo This script will help you complete the Spleeter integration.
echo Make sure you have Python installed before proceeding.
echo.
echo Step 1: Install Python dependencies and build executable
echo ---------------------------------------------------------
cd /d "%~dp0"
if exist "spleeter_wrapper.py" (
    echo Installing Spleeter and PyInstaller...
    pip install spleeter pyinstaller
    
    echo Building standalone executable...
    python -m PyInstaller --onefile --clean spleeter_wrapper.py
    
    if exist "dist\spleeter_wrapper.exe" (
        echo ✓ Spleeter executable built successfully!
        
        echo Step 2: Copying to WPF output directory...
        set "output_dir=..\bin\Debug\net8.0-windows\Resources"
        if not exist "%output_dir%" mkdir "%output_dir%"
        copy "dist\spleeter_wrapper.exe" "%output_dir%\"
        
        echo ✓ Executable copied to WPF output directory
        
        echo Step 3: Cleaning up build files...
        if exist "build" rmdir /s /q "build"
        if exist "dist" rmdir /s /q "dist"
        if exist "spleeter_wrapper.spec" del "spleeter_wrapper.spec"
        
        echo.
        echo ========================================
        echo   🎉 SETUP COMPLETE! 🎉
        echo ========================================
        echo.
        echo Your WPF music player now has:
        echo ✓ Karaoke mode (🎤 button)
        echo ✓ Stem export (💾 button)
        echo ✓ Offline AI audio separation
        echo.
        echo Next steps:
        echo 1. Build your WPF project: dotnet build
        echo 2. Run your application: dotnet run
        echo 3. Test karaoke mode with any song
        echo 4. Try exporting stems using the save button
        echo.
        echo Note: First separation will take ~30 seconds
        echo      Subsequent separations will be instant (cached)
        echo.
    ) else (
        echo ✗ Failed to build executable. Check for errors above.
    )
) else (
    echo ✗ spleeter_wrapper.py not found!
    echo Please run this script from the Resources folder.
)

echo.
pause
