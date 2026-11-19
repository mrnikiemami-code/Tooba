# RECEIPTS — TB-282-R16

## Claim
- `/api/tasks/next` → `514e987d-00c8-4421-b5b8-57867d02599f`
- Base `d7c36bf83…` ancestor of claim `351c0e73e…`

## Before-fix
```
npx tsx --test …story-management-282r16.mount.test.ts
→ ✖ (~2400ms) under pre-stabilization / unstable grid inputs
```

## After-fix
```
npx tsx --test …282r16… → .
npx tsx --test …282r14… → ....
npx tsx --test …282r15… → ..
npx tsc --noEmit → 0
```

## Served (controlled HTTP on real next-dev UI)
```
baseline=1 afterOpen=1 afterType=1
selectedBefore=selectedAfter=انتخاب‌شده: ۳
eligible=2 skipped=1
afterCancel rejectPosts=0 inputGone=true
ok=true
```
Host5088 `/health` 200, `/health/ready` SingleStore+messaging Healthy; Host DLL `D5DF9070D5C8B51F5B3DAD5BD0847751B10A1AE4DF5DD2C550AAB8F1E0599BEE`.

## Facts
1. Open Reject + Persian char-by-char + backspace: zero new `/stories/query` reads; selection/focus/eligibility stable
2. Cancel: zero reject POST; selection retained; reopen+confirm dispatches only 2 eligible versions 21/22 with distinct occurrence keys; ineligible «موبایل» never POSTed
3. Review-filter change still fetches; AppDataGrid + VendorShell unchanged

## Source
- `StoryManagementScreen.tsx`: stable grid callbacks + LiveGrid refs; queryAdapter deps mode/reloadToken/reviewFilter
