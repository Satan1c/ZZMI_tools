#!/bin/bash

VERSION="${GITHUB_REF#refs/tags/}"

# Generate release description with usage instructions
cat > RELEASE_NOTES.md << EOF
# Release $VERSION

## What This Does
Automatically updates asset hash references in your `.ini` files when game version changes. Maintains compatibility with ZZ Model Importer by tracking how character model data structures have changed across game versions.

## Installation

### Windows
1. Download \`VersionFixer.exe\` from the assets below
2. Copy it to your **Mods folder** (same folder as your `.ini` files)

### Linux
1. Download \`libVersionFixer.so\` from the assets below
2. Copy it to your **Mods folder** (same folder as your `.ini` files)

### macOS (Intel)
1. Download \`VersionFixer Intel.app\` from the assets below
2. Copy it to any folder (Desktop, Downloads, Mods folder, etc.)

### macOS (Apple Silicon M1/M2/M3)
1. Download \`VersionFixer Apple Silicon.app\` from the assets below
2. Copy it to any folder (Desktop, Downloads, Mods folder, etc.)

## Required Files

**IMPORTANT:** For VersionFixer to work, you must also have \`PlayerCharacterData.json\` in the same folder as the executable:

- **Windows/Linux:** Copy \`PlayerCharacterData.json\` to your Mods folder alongside the executable
- **macOS:** Drag both \`VersionFixer Intel.app\` (or Apple Silicon version) AND \`PlayerCharacterData.json\` to your Mods folder

The JSON file contains hash mappings for the specific game version you're using. Without it, VersionFixer cannot determine which hashes to update.

## How to Use

### Apply Fixes (Default Behavior)
Run the updater without arguments to apply all version fixes:
\`\`\`bash
# Windows
VersionFixer.exe

# Linux
./libVersionFixer.so

# macOS Intel
./VersionFixer Intel.app/Contents/MacOS/VersionFixer

# macOS Apple Silicon
./VersionFixer Apple Silicon.app/Contents/MacOS/VersionFixer
\`\`\`

### Specify Custom Mods Folder
If your `.ini` files are not in the current directory, use the \`--path\` flag:
\`\`\`bash
# Windows
VersionFixer.exe --path "/path/to/your/Mods"

# Linux
./libVersionFixer.so --path "/path/to/your/Mods"

# macOS
./VersionFixer Intel.app/Contents/MacOS/VersionFixer --path "/path/to/your/Mods"
\`\`\`

**Note:** When using \`--path\`, VersionFixer looks for \`PlayerCharacterData.json\` in that specified folder. Make sure the JSON file is there too!

### Undo All Previous Fixes
Revert all changes made by VersionFixer:
\`\`\`bash
# Windows
VersionFixer.exe undo

# Linux
./libVersionFixer.so undo

# macOS
./VersionFixer Intel.app/Contents/MacOS/VersionFixer undo
\`\`\`

## Logging Levels
- \`-l v\` — Verbose (show every change made)
- \`-l s\` — Standard (summary only, default)
- \`-l n\` — No logging

Example: \`VersionFixer.exe -l v --path "/Mods"\`

## Important Notes
- **Automatic Backups:** Original `.ini` files are backed up as \`DISABLED_versionfix_[timestamp].ini\` before any changes. You can restore them by running the undo command.
- **Game Version Compatibility:** This updater supports game version detected from \`PlayerCharacterData.json\`. Make sure you have the correct JSON file for your game version!
- **Manual Override:** You can always edit `.ini` files directly, but VersionFixer ensures consistency across all references (hash, object_indexes, object_index_counts).

EOF

# Append changelog from git log
echo "" >> RELEASE_NOTES.md
echo "## Changelog" >> RELEASE_NOTES.md
git log ${GITHUB_SHA}..HEAD --oneline 2>/dev/null >> RELEASE_NOTES.md || echo "No new commits since tag" >> RELEASE_NOTES.md
