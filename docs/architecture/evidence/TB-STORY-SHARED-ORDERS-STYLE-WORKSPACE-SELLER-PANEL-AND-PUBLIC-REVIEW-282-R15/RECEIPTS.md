# RECEIPTS — TB-282-R15

## Claim
- `/api/tasks/next` → `89dc664c-952a-448b-9834-117b4ef4a139`
- Base `8c6fe3632…` ancestor of `afb20cc8c…`

## Before-fix
```
npx tsx --test --test-name-pattern="rejected fetch" …282r15…
→ ✖ (~630ms) — result Dialog not reached under rejected fetch
```

## After-fix
```
npx tsx --test …282r15… …282r14…
→ 6/6 pass
npx tsc --noEmit → 0
```

## Facts
1. Select 2 eligible + 1 skipped; A 200 ok; B `throw TypeError("Failed to fetch")` → result Dialog: A موفق, B نامشخص, C ردشده; unhandledRejection=0; only B pending
2. Close via `story-admin-op-result-close`; recovery retry exact body bytes; A version not repeated
3. Rejection + epoch bump mid-first → B not_started; secondHits=0

## Source
- `story-executor.ts`: `transportRejectionResult` around JSON + FormData fetch
- `story-admin-ops.ts`: treat `story.transport.rejected` as unknown
