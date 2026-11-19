# RECEIPTS — TB-282-R17

## Claim
- `/api/tasks/next` → `61e50bd8-bdee-4e59-9aea-f5cee05cecee`
- Base `6271b64a5…` ancestor of `6a380ecf0…`

## After-fix
```
R17 mount … → ...
R14 … → ....
R15 … → ..
R16 … → .
R13 … → ........
tsc --noEmit → 0
```

## Served controlled (real next-dev UI)
```
zero-eligible approve toast: استوری‌های ویترین اصلی به تأیید نیاز ندارند.
dialogAfterZero=false approvePosts=0
mixed reject confirm: processable=قابل اجرا: ۲ skippedGroups=1 hasTitles=false
cancel → rejectPosts=0 dialogGone=true
```

## Source
- `story-admin-ops.ts`: explainZeroEligibleToast, groupSkippedReasons, formatAdminBatchToast
- `StoryManagementScreen.tsx`: classify-before-open; compact confirm; toast-only completion
- `story-management-copy.ts`: fa/en confirm button + skip headings
