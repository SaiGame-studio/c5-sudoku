@echo off
REM Unlock Android build settings if you need to update them
cd /d "%~dp0"

git update-index --no-skip-worktree "ProjectSettings/EditorBuildSettings.asset"
git update-index --no-skip-worktree "ProjectSettings/ProjectSettings.asset"

echo Android build settings unlocked!
echo You can now update these files if needed.
pause
