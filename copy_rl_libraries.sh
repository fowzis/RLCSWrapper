#!/bin/bash
# Bash script to copy RL libraries from rl project install/bin to RLlib folders
# This script calls the RLlib/copy_rl_libraries.sh script for convenience
# Usage: ./copy_rl_libraries.sh [RL_INSTALL_PATH] [--include-dependencies]

set -e

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RL_LIB_SCRIPT="$SCRIPT_DIR/RLlib/copy_rl_libraries.sh"

if [ ! -f "$RL_LIB_SCRIPT" ]; then
    echo "Error: RLlib copy script not found at: $RL_LIB_SCRIPT" >&2
    exit 1
fi

# Make sure the script is executable
chmod +x "$RL_LIB_SCRIPT"

# Call the RLlib script with all arguments
"$RL_LIB_SCRIPT" "$@"

