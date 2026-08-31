#!/bin/bash

VERSION="${GITHUB_REF#refs/tags/}"

cat > RELEASE_NOTES.md << EOF
# Release $VERSION

## What This Does
Automatically updates asset hash references in your `.ini` files when game version changes. Maintains compatibility with ZZ Model Importer by tracking how character model data structures have changed across game versions.

## Installation

### Windows
1. Download \`VersionFixer.exe\` from the assets below
2. Copy it to your **Mods folder** (same folder as your `.ini` files)
3. Double-click to run, or use from command line:
   ```bash
   VersionFixer.exe
   ```

### Linux
1. Download \`libVersionFixer.so\` from the assets below
2. Copy it to your **Mods folder** (same folder as your `.ini` files)
3. Make it executable:
   ```bash
   chmod +x libVersionFixer.so
   ```
4. Run from command line:
   ```bash
   ./libVersionFixer.so
   ```

### macOS (Intel)
1. Download \`VersionFixer Intel.app\` from the assets below
2. Copy it to any folder (Desktop, Downloads, Mods folder, etc.)
3. Double-click to run

### macOS (Apple Silicon M1/M2/M3)
1. Download \`VersionFixer Apple Silicon.app\` from the assets below
2. Copy it to any folder (Desktop, Downloads, Mods folder, etc.)
3. Double-click to run

## How to Use

### Apply Fixes (Default Behavior)
Run the updater without arguments to apply all version fixes:
```bash
# Windows
VersionFixer.exe

# Linux
./libVersionFixer.so

# macOS Intel
./VersionFixer Intel.app/Contents/MacOS/VersionFixer

# macOS Apple Silicon
./VersionFixer Apple Silicon.app/Contents/MacOS/VersionFixer
```

### Specify Custom Mods Folder
If your `.ini` files are not in the current directory:
```bash
# Windows
VersionFixer.exe --path "/path/to/your/Mods"

# Linux
./libVersionFixer.so --path "/path/to/your/Mods"

# macOS
./VersionFixer Intel.app/Contents/MacOS/VersionFixer --path "/path/to/your/Mods"
```

### Undo All Previous Fixes
Revert all changes made by VersionFixer:
```bash
# Windows
VersionFixer.exe undo

# Linux
./libVersionFixer.so undo

# macOS
./VersionFixer Intel.app/Contents/MacOS/VersionFixer undo
```

## Logging Levels
- \`-l v\` — Verbose (show every change made)
- \`-l s\` — Standard (summary only, default)
- \`-l n\` — No logging

## Important Notes
- **Automatic Backups:** Original `.ini` files are backed up as \`DISABLED_versionfix_[timestamp].ini\` before any changes
- **Game Version Compatibility:** This updater supports game version detected from \`PlayerCharacterData.json\`
- **Manual Override:** You can always edit `.ini` files directly, but VersionFixer ensures consistency across all references

## Changelog
EOF

git log ${GITHUB_SHA}..HEAD --oneline 2>/dev/null >> RELEASE_NOTES.md || echo "No new commits since tag" >> RELEASE_NOTES.md
