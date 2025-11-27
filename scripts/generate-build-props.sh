#!/bin/bash
# Generate directory-specific Directory.Build.props files with dynamic target framework values
# based on .NET SDK version. If any files differ from what's in git, commit and push, then exit with error.

set -e

# Create a temporary project to query SDK properties
TEMP_DIR=$(mktemp -d)
TEMP_PROJ="$TEMP_DIR/temp.csproj"

cat > "$TEMP_PROJ" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
EOF

# Get NETCoreAppMaximumVersion from SDK
MAX_VERSION=$(dotnet msbuild "$TEMP_PROJ" -getProperty:NETCoreAppMaximumVersion 2>/dev/null | tail -1 | tr -d ' ')

# Clean up temp project
rm -rf "$TEMP_DIR"

# Extract major version (e.g., "10.0" -> "10")
MAX_MAJOR=$(echo "$MAX_VERSION" | cut -d. -f1)

# Determine minimum supported major version based on SDK version
# .NET 8 LTS until Nov 2026, .NET 10 LTS until Nov 2028
if [ "$MAX_MAJOR" -eq 9 ] || [ "$MAX_MAJOR" -eq 10 ]; then
    MIN_MAJOR=8
elif [ "$MAX_MAJOR" -eq 11 ] || [ "$MAX_MAJOR" -eq 12 ]; then
    MIN_MAJOR=10
elif [ "$MAX_MAJOR" -eq 13 ]; then
    MIN_MAJOR=11
else
    # Fallback for unknown versions
    MIN_MAJOR=$MAX_MAJOR
fi

# Build list of supported frameworks
FRAMEWORKS=""
for v in $(seq $MIN_MAJOR $MAX_MAJOR); do
    if [ -n "$FRAMEWORKS" ]; then
        FRAMEWORKS="${FRAMEWORKS};net${v}.0"
    else
        FRAMEWORKS="net${v}.0"
    fi
done

CHANGES_MADE=false

# Generate src/Directory.Build.props for library projects (multi-targeting)
SRC_PROPS="src/Directory.Build.props"
TEMP_SRC=$(mktemp)
cat > "$TEMP_SRC" << EOF
<Project>
  <!-- Import parent props -->
  <Import Project="\$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '\$(MSBuildThisFileDirectory)../'))" />

  <!-- Library projects multi-target all non-EOL .NET versions -->
  <!-- Auto-generated based on SDK version by scripts/generate-build-props.sh -->
  <PropertyGroup>
    <TargetFrameworks>$FRAMEWORKS</TargetFrameworks>
  </PropertyGroup>
</Project>
EOF

if ! diff -q "$SRC_PROPS" "$TEMP_SRC" > /dev/null 2>&1; then
    echo "$SRC_PROPS needs updating for current .NET SDK version"
    mv "$TEMP_SRC" "$SRC_PROPS"
    CHANGES_MADE=true
else
    rm -f "$TEMP_SRC"
fi

# Function to generate single-target props file
generate_single_target_props() {
    local TARGET_FILE="$1"
    local TEMP_FILE=$(mktemp)

    cat > "$TEMP_FILE" << EOF
<Project>
  <!-- Import parent props -->
  <Import Project="\$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '\$(MSBuildThisFileDirectory)../'))" />

  <!-- Non-library projects target only the latest .NET version -->
  <!-- Auto-generated based on SDK version by scripts/generate-build-props.sh -->
  <PropertyGroup>
    <TargetFramework>net${MAX_MAJOR}.0</TargetFramework>
  </PropertyGroup>
</Project>
EOF

    if ! diff -q "$TARGET_FILE" "$TEMP_FILE" > /dev/null 2>&1; then
        echo "$TARGET_FILE needs updating for current .NET SDK version"
        mv "$TEMP_FILE" "$TARGET_FILE"
        CHANGES_MADE=true
    else
        rm -f "$TEMP_FILE"
    fi
}

# Generate tests/Directory.Build.props
generate_single_target_props "tests/Directory.Build.props"

# Generate src/applications/Directory.Build.props if the directory exists
if [ -d "src/applications" ]; then
    generate_single_target_props "src/applications/Directory.Build.props"
fi

# If changes were made and we're in CI, commit and push
if [ "$CHANGES_MADE" = true ]; then
    if [ -d .git ] && [ -n "$CI" ]; then
        git config user.name "github-actions[bot]"
        git config user.email "github-actions[bot]@users.noreply.github.com"
        git add src/Directory.Build.props tests/Directory.Build.props
        [ -d "src/applications" ] && git add src/applications/Directory.Build.props
        git commit -m "Update Directory.Build.props files for .NET SDK version"
        git push
        echo "ERROR: Directory.Build.props files were out of date and have been updated."
        echo "The changes have been committed and pushed. Please retry the workflow."
        exit 1
    else
        echo "Updated props files locally. Please commit the changes."
        exit 0
    fi
else
    echo "All Directory.Build.props files are up to date"
    exit 0
fi
