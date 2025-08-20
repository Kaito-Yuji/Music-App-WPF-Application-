#!/usr/bin/env python3
"""
Audio Separator Wrapper for WPF Music Player
Separates audio into vocals and accompaniment using modern audio-separator library
"""

import sys
import os
import argparse
import warnings
import re
from pathlib import Path

# Suppress common warnings
warnings.filterwarnings("ignore", category=UserWarning)
warnings.filterwarnings("ignore", category=FutureWarning)
warnings.filterwarnings("ignore", category=DeprecationWarning)

def sanitize_filename(filename):
    """Sanitize filename by removing or replacing problematic characters"""
    if not filename:
        return "unknown"
    
    # Remove or replace invalid characters
    sanitized = re.sub(r'[<>:"/\\|?*]', '_', filename)
    sanitized = re.sub(r'[()[\]{}]', '_', sanitized)
    sanitized = re.sub(r'[&%$#@!]', '_', sanitized)
    
    # Remove consecutive underscores
    sanitized = re.sub(r'_+', '_', sanitized)
    
    # Remove leading/trailing underscores and limit length
    sanitized = sanitized.strip('_')[:30]
    
    return sanitized if sanitized else "audio_file"

def main():
    parser = argparse.ArgumentParser(description='Separate audio into vocals and accompaniment')
    parser.add_argument('input_file', help='Input audio file path (can be absolute or relative)')
    parser.add_argument('output_dir', help='Output directory for separated stems')
    parser.add_argument('--stems', choices=['2', '4', '5'], default='2', 
                       help='Number of stems to separate (default: 2)')
    
    if len(sys.argv) < 3:
        parser.print_help()
        sys.exit(1)
    
    args = parser.parse_args()
    
    # Convert input file to absolute path if it's not already
    if not os.path.isabs(args.input_file):
        # If relative path, try to resolve it relative to current working directory
        input_file = Path(os.path.abspath(args.input_file))
    else:
        input_file = Path(args.input_file)
    
    output_dir = Path(args.output_dir)
    
    # Validate input file
    if not input_file.exists():
        print(f"Error: Input file '{input_file}' does not exist", file=sys.stderr)
        sys.exit(1)
    
    # Validate input file format
    supported_formats = {'.mp3', '.wav', '.flac', '.aac', '.ogg', '.m4a', '.wma'}
    if input_file.suffix.lower() not in supported_formats:
        print(f"Error: Unsupported file format '{input_file.suffix}'", file=sys.stderr)
        print(f"Supported formats: {', '.join(supported_formats)}", file=sys.stderr)
        sys.exit(1)
    
    # Create unique output directory for this specific song
    song_name = sanitize_filename(input_file.stem)
    song_output_dir = output_dir / song_name
    temp_separation_dir = output_dir / f"temp_{song_name}"
    
    try:
        output_dir.mkdir(parents=True, exist_ok=True)
        song_output_dir.mkdir(parents=True, exist_ok=True)
        temp_separation_dir.mkdir(parents=True, exist_ok=True)
    except Exception as e:
        print(f"Error creating output directory: {e}", file=sys.stderr)
        sys.exit(1)
    
    # Import audio-separator
    try:
        from audio_separator.separator import Separator
        print("Progress: 5%")
        print("Audio Separator loaded successfully")
    except ImportError as e:
        print(f"Error: Audio Separator not installed or not found", file=sys.stderr)
        print(f"Please install with: pip install audio-separator", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"Error loading Audio Separator: {e}", file=sys.stderr)
        sys.exit(1)
    
    # Initialize separator
    try:
        print(f"Progress: 10%")
        print(f"Initializing audio separator...")
        
        # Create separator instance with unique temp directory to avoid file conflicts
        separator = Separator(output_dir=str(temp_separation_dir))
        
        print("Progress: 15%")
        # Load a model that can separate vocals and accompaniment
        # Check available models and use a simple approach
        try:
            # Try to load a basic model for vocal separation
            separator.load_model()  # Load default model
            print("Default model loaded successfully")
        except Exception as model_error:
            print(f"Model loading issue: {model_error}")
            # Try with specific model file
            separator.load_model(model_filename="UVR-MDX-NET-Inst_HQ_3.onnx")
            print("Specific model loaded successfully")
        
    except Exception as e:
        print(f"Error initializing Audio Separator: {e}", file=sys.stderr)
        print("", file=sys.stderr)
        print("SOLUTION: Audio separation setup failed.", file=sys.stderr)
        print("This might be due to missing models or network issues.", file=sys.stderr)
        print("Please ensure you have internet connection for model download.", file=sys.stderr)
        print("", file=sys.stderr)
        sys.exit(1)
    
    # Perform separation
    try:
        print(f"Starting separation process for '{input_file.name}'...")
        print("Progress: 20%")
        
        # Perform the separation - audio-separator typically separates into individual files
        print("Loading audio file and starting separation...")
        print("Progress: 30%")
        
        output_files = separator.separate(str(input_file))
        
        print("Progress: 70%")
        print("Separation processing completed!")
        
        # Look for separated files in the temp directory
        if temp_separation_dir.exists():
            print("Progress: 80%")
            all_files = list(temp_separation_dir.rglob('*.wav'))
            if not all_files:
                all_files = list(temp_separation_dir.rglob('*.*'))  # Check for any files
            
            print(f"Found {len(all_files)} separated files")
            print("Progress: 85%")
            for file_path in all_files:
                print(f"  - {file_path}")
                
            # Move the separated files to the final song directory
            if all_files:
                import shutil
                
                print("Progress: 90%")
                print("Saving separated tracks...")
                
                for file_path in all_files:
                    # Determine target name based on content
                    if any(keyword in file_path.name.lower() for keyword in ['vocal', 'voice']):
                        target_name = "vocals.wav"
                    elif any(keyword in file_path.name.lower() for keyword in ['instrumental', 'accompan', 'music']):
                        target_name = "accompaniment.wav"
                    else:
                        target_name = file_path.name
                    
                    target_path = song_output_dir / target_name
                    
                    # Copy the file (not move, to preserve the original)
                    shutil.copy2(str(file_path), str(target_path))
                    print(f"Saved {target_name}")
                
                print("Progress: 95%")
                # Clean up temp directory
                try:
                    shutil.rmtree(str(temp_separation_dir))
                    print(f"Cleanup completed")
                except Exception as cleanup_error:
                    print(f"Warning: Could not clean up temp directory: {cleanup_error}")
                    
                print("Progress: 100%")
                print(f"Audio separation completed successfully!")
                print(f"Separated files saved to: {song_output_dir}")
            else:
                print("Warning: No separated files found", file=sys.stderr)
        else:
            print("Warning: Temp separation directory not found", file=sys.stderr)
            
    except Exception as e:
        print(f"Error during separation: {e}", file=sys.stderr)
        import traceback
        print("Full error details:", file=sys.stderr)
        traceback.print_exc(file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
