# PyInstaller Guide for Spleeter Wrapper

## Overview

This guide explains how to rebuild the `spleeter_wrapper.exe` with embedded Spleeter models using PyInstaller.

## Prerequisites

- Python 3.13+ installed
- PyInstaller package installed
- Spleeter models downloaded in `spleeter_models/` folder

## Command Explanation

The PyInstaller command used:

```bash
pyinstaller --onefile spleeter_wrapper.py --add-data "spleeter_models;spleeter_models" --name spleeter_wrapper_with_models
```

### Parameters Breakdown:

- `--onefile`: Creates a single executable file (instead of a folder with multiple files)
- `spleeter_wrapper.py`: The Python script to convert to executable
- `--add-data "spleeter_models;spleeter_models"`: Embeds the models folder into the executable
  - Format on Windows: `"source_path;destination_path"`
  - Format on Linux/Mac: `"source_path:destination_path"`
- `--name spleeter_wrapper_with_models`: Custom name for the output executable

## Step-by-Step Process

1. **Navigate to Resources directory:**

   ```powershell
   cd "d:\SchoolWork\PRN212\Music\Music-App-WPF-Application-\WpfMusicPlayer\Resources"
   ```

2. **Install PyInstaller (if not already installed):**

   ```powershell
   pip install pyinstaller
   ```

3. **Build the executable with embedded models:**

   ```powershell
   pyinstaller --onefile spleeter_wrapper.py --add-data "spleeter_models;spleeter_models" --name spleeter_wrapper_with_models
   ```

4. **Replace the current executable:**
   ```powershell
   Copy-Item ".\dist\spleeter_wrapper_with_models.exe" ".\spleeter_wrapper.exe" -Force
   ```

## Benefits of Embedded Models

- **Offline Operation**: No internet required after first build
- **Portability**: Single executable file contains everything needed
- **Faster Startup**: Models load from memory instead of disk
- **No External Dependencies**: Users don't need to download models separately

## File Structure After Build

```
Resources/
├── spleeter_wrapper.py          # Source Python script
├── spleeter_wrapper.exe         # Updated executable with embedded models
├── spleeter_models/             # Original model files (still needed for building)
│   └── 2stems/                  # 2-stem separation model
├── dist/                        # PyInstaller output directory
│   └── spleeter_wrapper_with_models.exe
├── build/                       # PyInstaller build cache
└── spleeter_wrapper_with_models.spec  # PyInstaller spec file
```

## Testing

Test the executable:

```powershell
.\spleeter_wrapper.exe --help
```

You should see:

```
Using offline Spleeter models at: C:\Users\...\AppData\Local\Temp\_MEI...\spleeter_models
```

This confirms the models are embedded and being used offline.

## Troubleshooting

- **"pyinstaller not found"**: Install with `pip install pyinstaller`
- **Missing models**: Ensure `spleeter_models/2stems/` contains the model files
- **Large file size**: Normal - embedded models make the exe larger (~100MB+)
- **Slow first run**: Models are extracted to temp directory on first execution

## Model Types Supported

- **2stems**: Separates into vocals and accompaniment
- **4stems**: Separates into vocals, drums, bass, other
- **5stems**: Separates into vocals, drums, bass, piano, other

To support other stems, download the respective models to `spleeter_models/` and rebuild.
