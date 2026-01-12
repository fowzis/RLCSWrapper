#!/bin/bash
# Bash script to copy RL libraries from rl project install/bin to RLlib folders
# Usage: ./copy_rl_libraries.sh [RL_INSTALL_PATH] [--include-dependencies]

set -e

# Get script directory (RLlib folder)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WINDOWS_PATH="$SCRIPT_DIR/Windows"
LINUX_PATH="$SCRIPT_DIR/Linux"
MACOS_PATH="$SCRIPT_DIR/macOS"

# Parse arguments
RL_INSTALL_PATH=""
INCLUDE_DEPS=false

for arg in "$@"; do
    case $arg in
        --include-dependencies)
            INCLUDE_DEPS=true
            shift
            ;;
        *)
            if [ -z "$RL_INSTALL_PATH" ]; then
                RL_INSTALL_PATH="$arg"
            fi
            shift
            ;;
    esac
done

# Determine source path
if [ -z "$RL_INSTALL_PATH" ]; then
    # Try to find RL install directory relative to this script
    # Assuming RLTrajectoryPlanner is sibling to rl project
    PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"
    RL_INSTALL_PATH="$PROJECT_ROOT/rl/install"
fi

BIN_PATH="$RL_INSTALL_PATH/bin"

# Create target directories
echo "Creating target directories..."
mkdir -p "$WINDOWS_PATH"
mkdir -p "$LINUX_PATH"
mkdir -p "$MACOS_PATH"

# Check if source directory exists
if [ ! -d "$BIN_PATH" ]; then
    echo "Error: RL install/bin directory not found at: $BIN_PATH" >&2
    echo ""
    echo "Usage: ./copy_rl_libraries.sh [RL_INSTALL_PATH] [--include-dependencies]" >&2
    echo "       ./copy_rl_libraries.sh /path/to/rl/install --include-dependencies" >&2
    exit 1
fi

echo "Source directory: $BIN_PATH"
echo "Target directories:"
echo "  Windows: $WINDOWS_PATH"
echo "  Linux:   $LINUX_PATH"
echo "  macOS:   $MACOS_PATH"
echo ""

# Copy Windows DLLs (rl*.dll)
echo "Copying Windows libraries (rl*.dll)..."
WINDOWS_COUNT=0
if ls "$BIN_PATH"/rl*.dll 1> /dev/null 2>&1; then
    for file in "$BIN_PATH"/rl*.dll; do
        filename=$(basename "$file")
        cp "$file" "$WINDOWS_PATH/"
        echo "  Copied $filename"
        ((WINDOWS_COUNT++))
    done
    echo "  Copied $WINDOWS_COUNT RL library file(s) to Windows folder"
else
    echo "  No rl*.dll files found in $BIN_PATH"
fi

# Copy dependency DLLs if requested
if [ "$INCLUDE_DEPS" = true ]; then
    echo ""
    echo "Copying Windows dependency libraries..."
    
    # Visual C++ Runtime DLLs (if on Windows)
    if ls "$BIN_PATH"/msvcp*.dll 1> /dev/null 2>&1; then
        for file in "$BIN_PATH"/msvcp*.dll "$BIN_PATH"/vcruntime*.dll "$BIN_PATH"/concrt*.dll; do
            if [ -f "$file" ]; then
                filename=$(basename "$file")
                cp "$file" "$WINDOWS_PATH/"
                echo "  Copied dependency: $filename"
            fi
        done
    fi
    
    # Boost DLLs
    if ls "$BIN_PATH"/boost_*.dll 1> /dev/null 2>&1; then
        for file in "$BIN_PATH"/boost_*.dll; do
            filename=$(basename "$file")
            cp "$file" "$WINDOWS_PATH/"
            echo "  Copied dependency: $filename"
        done
    fi
    
    # libxml2 DLLs
    if ls "$BIN_PATH"/*xml*.dll 1> /dev/null 2>&1; then
        for file in "$BIN_PATH"/*xml*.dll; do
            filename=$(basename "$file")
            cp "$file" "$WINDOWS_PATH/"
            echo "  Copied dependency: $filename"
        done
    fi
fi

# Copy Linux .so files (librl*.so)
echo ""
echo "Copying Linux libraries (librl*.so)..."
LINUX_COUNT=0
if ls "$BIN_PATH"/librl*.so* 1> /dev/null 2>&1; then
    for file in "$BIN_PATH"/librl*.so*; do
        filename=$(basename "$file")
        cp "$file" "$LINUX_PATH/"
        echo "  Copied $filename"
        ((LINUX_COUNT++))
    done
    echo "  Copied $LINUX_COUNT RL library file(s) to Linux folder"
else
    echo "  No librl*.so files found in $BIN_PATH"
    echo "  (This is normal if building on Windows)"
fi

# Copy macOS .dylib files (librl*.dylib)
echo ""
echo "Copying macOS libraries (librl*.dylib)..."
MACOS_COUNT=0
if ls "$BIN_PATH"/librl*.dylib* 1> /dev/null 2>&1; then
    for file in "$BIN_PATH"/librl*.dylib*; do
        filename=$(basename "$file")
        cp "$file" "$MACOS_PATH/"
        echo "  Copied $filename"
        ((MACOS_COUNT++))
    done
    echo "  Copied $MACOS_COUNT RL library file(s) to macOS folder"
else
    echo "  No librl*.dylib files found in $BIN_PATH"
    echo "  (This is normal if building on Windows)"
fi

echo ""
echo "Done! Libraries copied successfully."
echo ""
echo "Summary:"
echo "  Windows: $WINDOWS_PATH"
if [ $WINDOWS_COUNT -gt 0 ]; then
    echo "    - $WINDOWS_COUNT RL library file(s)"
fi
echo "  Linux:   $LINUX_PATH"
if [ $LINUX_COUNT -gt 0 ]; then
    echo "    - $LINUX_COUNT RL library file(s)"
fi
echo "  macOS:   $MACOS_PATH"
if [ $MACOS_COUNT -gt 0 ]; then
    echo "    - $MACOS_COUNT RL library file(s)"
fi
echo ""
echo "Note: If you need dependency libraries (Boost, libxml2, etc.)," >&2
echo "      run with --include-dependencies flag or copy them manually." >&2
