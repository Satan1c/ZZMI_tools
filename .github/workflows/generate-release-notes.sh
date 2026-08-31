#!/bin/bash

VERSION="${GITHUB_REF#refs/tags/}"

cat > RELEASE_NOTES.md << EOF
# Release $VERSION

## What This Does
Automatically updates asset hash references in your `.ini` files when game version changes. Maintains compatibility with ZZ Model Importer by tracking how character model data structures have changed across game versions.

## Installation

### Windows
1. Download \`VersionFixer-win-x64.exe\` from the assets below
2. Copy it to your **Mods folder** (same folder as your `.ini` files)

### Linux
1. Download \`VersionFixer-linux-x64\` from the assets below
2. Copy it to your **Mods folder** (same folder as your `.ini` files)
3. Make it executable: \`chmod +x VersionFixer-linux-x64\`

### macOS (Intel)
1. Download \`VersionFixer-macos-x64.zip\` from the assets below
2. Extract the zip file
3. Move \`VersionFixer.app\` to any folder (Desktop, Downloads, Mods folder, etc.)

### macOS (Apple Silicon M1/M2/M3)
1. Download \`VersionFixer-macos-arm64.zip\` from the assets below
2. Extract the zip file
3. Move \`VersionFixer.app\` to any folder (Desktop, Downloads, Mods folder, etc.)

## Required Files

**IMPORTANT:** For VersionFixer to work, you must also have \`PlayerCharacterData.json\` in the same folder as the executable:

- **Windows/Linux:** Copy \`PlayerCharacterData.json\` to your Mods folder alongside the executable
- **macOS:** Drag both \`VersionFixer.app\` AND \`PlayerCharacterData.json\` to your Mods folder

The JSON file contains hash mappings for the specific game version you're using. Without it, VersionFixer cannot determine which hashes to update.

## How to Use

### Apply Fixes (Default Behavior)
Run the updater without arguments to apply all version fixes:
\`\`\`bash
# Windows
VersionFixer-win-x64.exe

# Linux
./VersionFixer-linux-x64

# macOS Intel
./VersionFixer.app/Contents/MacOS/VersionFixer

# macOS Apple Silicon
./VersionFixer.app/Contents/MacOS/VersionFixer
\`\`\`

### Specify Custom Mods Folder
If your `.ini` files are not in the current directory, use the \`--path\` flag:
\`\`\`bash
# Windows
VersionFixer-win-x64.exe --path "/path/to/your/Mods"

# Linux
./VersionFixer-linux-x64 --path "/path/to/your/Mods"

# macOS
./VersionFixer.app/Contents/MacOS/VersionFixer --path "/path/to/your/Mods"
\`\`\`

**Note:** When using \`--path\`, VersionFixer looks for \`PlayerCharacterData.json\` in that specified folder. Make sure the JSON file is there too!

### Undo All Previous Fixes
Revert all changes made by VersionFixer:
\`\`\`bash
# Windows
VersionFixer-win-x64.exe undo

# Linux
./VersionFixer-linux-x64 undo

# macOS
./VersionFixer.app/Contents/MacOS/VersionFixer undo
\`\`\`

## Logging Levels
- \`-l v\` — Verbose (show every change made)
- \`-l s\` — Standard (summary only, default)
- \`-l n\` — No logging

Example: \`VersionFixer-win-x64.exe -l v --path "/Mods"\`

## Important Notes
- **Automatic Backups:** Original `.ini` files are backed up as \`DISABLED_versionfix_[timestamp].ini\` before any changes. You can restore them by running the undo command.
- **Game Version Compatibility:** This updater supports game version detected from \`PlayerCharacterData.json\`. Make sure you have the correct JSON file for your game version!
- **Manual Override:** You can always edit `.ini` files directly, but VersionFixer ensures consistency across all references (hash, object_indexes, object_index_counts).

EOF

echo "" >> RELEASE_NOTES.md
echo "## Changelog" >> RELEASE_NOTES.md
git log ${GITHUB_SHA}..HEAD --oneline 2>/dev/null >> RELEASE_NOTES.md || echo "No new commits since tag" >> RELEASE_NOTES.md
