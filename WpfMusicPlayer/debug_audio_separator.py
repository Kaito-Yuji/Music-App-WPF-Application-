#!/usr/bin/env python3
"""
Test script to debug audio separator availability
"""
import sys
import os
from pathlib import Path

def test_audio_separator():
    print("=== Audio Separator Availability Test ===")
    print(f"Python version: {sys.version}")
    print(f"Python executable: {sys.executable}")
    
    # Test import
    try:
        from audio_separator.separator import Separator
        print("✓ audio_separator imported successfully")
    except ImportError as e:
        print(f"✗ Failed to import audio_separator: {e}")
        return False
    
    # Test separator creation
    try:
        separator = Separator()
        print("✓ Separator instance created successfully")
    except Exception as e:
        print(f"✗ Failed to create Separator instance: {e}")
        return False
    
    # Test model loading
    try:
        # Try a simpler, more reliable model first
        separator.load_model(model_filename="UVR-MDX-NET-Inst_HQ_3.onnx")
        print("✓ UVR-MDX-NET-Inst_HQ_3 model loaded successfully")
    except Exception as e:
        print(f"✗ Failed to load UVR-MDX-NET-Inst_HQ_3 model: {e}")
        try:
            # Fallback to default model
            separator.load_model()
            print("✓ Default model loaded successfully")
        except Exception as e2:
            print(f"✗ Failed to load default model: {e2}")
            return False
    
    print("✓ All tests passed - audio_separator is ready!")
    return True

if __name__ == "__main__":
    success = test_audio_separator()
    sys.exit(0 if success else 1)
