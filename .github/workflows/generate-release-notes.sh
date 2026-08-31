#!/bin/bash

VERSION="${GITHUB_REF#refs/tags/}"

cat > RELEASE_NOTES.md << EOF
## Usage

### Windows
1. Place \`PlayerCharacterData.json\` in Mods folder
2. Double-click \`VersionFixer-win-x64.exe\`

### Linux x64
1. Place \`PlayerCharacterData.json\` in Mods folder
2. Download \`VersionFixer-linux-x64\` from release assets
3. Make executable: \`chmod +x VersionFixer-linux-x64\`
4. Run: \`./VersionFixer-linux-x64\`

### macOS x64 (Intel)
1. Place \`PlayerCharacterData.json\` in Mods folder
2. Download and extract \`VersionFixer-macos-x64.zip\` from release assets
3. Run: \`./VersionFixer.app/Contents/MacOS/VersionFixer\`

### macOS ARM64 (Apple Silicon)
1. Place \`PlayerCharacterData.json\` in Mods folder
2. Download and extract \`VersionFixer-macos-arm64.zip\` from release assets
3. Run: \`./VersionFixer.app/Contents/MacOS/VersionFixer\`

---

### .cs script (alternative)
**Prerequisites:**
- Requires [.NET 10 SDK](https://dotnet.microsoft.com/download) or later

**Steps:**
1. Place \`PlayerCharacterData.json\` in Mods folder
2. Open terminal/CMD in project directory
3. Run: \`dotnet run VersionFixer.cs\`

**Notes:**
- Downloads .NET runtime automatically on first run if missing
- Outputs to console, no GUI
- Same functionality as compiled binaries
EOF

git log ${GITHUB_SHA}..HEAD --oneline 2>/dev/null >> RELEASE_NOTES.md || echo "No new commits since tag" >> RELEASE_NOTES.md
