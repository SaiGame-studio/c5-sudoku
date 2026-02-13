# Android Build - Git Workflow Guide

## Vấn đề
Khi pull code từ nhánh PC, Unity tự động chuyển platform và mất build settings cho Android.

## Giải pháp

### Bước 1: Lock build settings (Chỉ chạy 1 lần)
Sau khi đã chỉnh platform về Android, chạy file:
```
lock-android-settings.bat
```

File này sẽ báo Git bỏ qua các thay đổi từ PC branch cho các file:
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/ProjectSettings.asset`

### Bước 2: Pull code bình thường
Từ giờ khi pull code từ PC, Git sẽ:
- ✅ Cập nhật tất cả code game (scripts, assets, scenes)
- ❌ KHÔNG cập nhật build settings (giữ nguyên Android)

### Nếu cần cập nhật build settings
Trong trường hợp đặc biệt bạn muốn cập nhật build settings từ PC:
1. Chạy `unlock-android-settings.bat`
2. Pull code
3. Chỉnh lại platform về Android
4. Chạy lại `lock-android-settings.bat`

## Kiểm tra status
Để xem các file đang bị lock:
```bash
git ls-files -v | findstr "^S"
```

## Technical Details
Sử dụng `git update-index --skip-worktree` để Git bỏ qua thay đổi local của các file platform-specific trong khi vẫn giữ chúng trong repository.
