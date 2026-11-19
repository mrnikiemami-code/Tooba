# RECEIPTS — TB-282-R14

## Claim
- `/api/tasks/next` → `8de2cc6e-afdf-4c1b-b50d-9e2381182693`
- Base `eae4b3196…` ancestor of claim HEAD `9596f6888…`

## Tests
```
npx tsx --test --test-timeout=20000 app/stories/management/story-management-282r14.mount.test.ts
→ 4/4
npx tsc --noEmit → 0
```

## Facts
1. Real AG Grid: select 3 rows (2 Submitted + 1 Approved) → bulk تأیید → only 2 `/approve` with versions 11/22 and distinct occurrenceKeys; skipped C visible; result panel lists A/B موفق + C ردشده
2. Partial: A ok + B empty 2xx → B unknown/pending; recovery retry exact same body; A not re-sent
3. Mid-batch epoch++ / actor change while first deferred → secondHits=0; B `not_started` «اجرا نشد»
4. Reject bulk: empty reason 0 POST; cancel 0; with reason 1 POST

## Serve
- Host `:5088` health 200 skip-bootstrap preserved
- Next `:3000` next-dev `/fa/admin/stories` 200
- ZERO live approve/reject/delete
