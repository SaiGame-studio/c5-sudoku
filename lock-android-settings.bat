@echo off
REM Lock Android build settings - run this once after setting platform to Android
cd /d "%~dp0"

git update-index --skip-worktree "ProjectSettings/EditorBuildSettings.asset"
git update-index --skip-worktree "ProjectSettings/ProjectSettings.asset"

echo Android build settings locked successfully!
echo These files will not be updated when pulling from PC branch.
pause
