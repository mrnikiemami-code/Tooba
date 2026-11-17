# TB-277-R2 — receipts

All commands run from `D:\Users\User\source\repos\SarvNewVer`. One run per verification step after the final edit,
except where a re-run is explicitly declared.

## 1. Wake, claim and scope

```
GET  http://127.0.0.1:17321/api/tasks?channelId=tooba-main
     -> afb1e8ac-f381-48f1-8cc6-d8c739470b77  TB-…-277-R2  Claimed  (claimedAtUtc 09:41:10Z)
GET  http://127.0.0.1:17321/api/tasks/next?channelId=tooba-main   -> 204 (row already claimed)
GET  /api/tasks?… row keys = id,channelId,taskId,status,createdAtUtc,claimedAtUtc,completedAtUtc  -> no content field
     (declared: the envelope text is not retrievable; TASK.md is reconstructed from the issued Architect review)
```

```
git fetch origin && git merge --ff-only origin/main
9adc53bd1 -> eaab66e86  docs(returns): accept quantity repair and isolate final uncertain-receipt classifier gap
```

## 2. The source change (3 files, no backend)

```
src/frontend/app/returns/return-review-intent.ts        PROVEN_NO_EFFECT_REFUSAL_CODES: -1 token (9 remain)
                                                       + doc block: what 277-R2 removed and why; body-only comment corrected
src/frontend/app/admin/admin-returns-work-queue.test.ts  assertion flipped to false + the reason it must stay false
src/frontend/app/admin/admin-return-workspace-277.mount.test.ts  +1 mounted fact + scripted-route/notice helpers
```

`git status --porcelain` → exactly those three paths (plus evidence docs).

## 3. Focused verification

```
cd src/frontend
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-277.mount.test.ts
  ✔ no review form is rendered inline … (119.8ms)
  ✔ an over-ceiling quantity is refused inside the receipt dialog … (21.8ms)
  ✔ the unit's own precision bounds the field … (36.6ms)
  ✔ usable and damaged portions of ONE line share the same ceiling (31.0ms)
  ✔ approve records the decision only, and the first refund is a separate reviewed command (35.6ms)
  ✔ a rejection reason shorter than three characters cannot be submitted (24.6ms)
  ✔ a case's items show their own stored disposition … (24.6ms)
  ✔ an unknown receipt send survives a unit-metadata refusal and only the exact replay resolves it (50.9ms)   <-- NEW
  ✔ the quantity guard is unit-aware and normalizes Persian input (0.3ms)
  ℹ tests 9  pass 9  fail 0  skipped 0

node --experimental-strip-types --test app/returns/return-quantity.test.ts \
  app/admin/admin-returns-work-queue.test.ts app/admin/admin-error-map.test.ts app/returns/return-api.test.ts
  ℹ tests 48  pass 48  fail 0

npx tsc --noEmit                 -> exit 0
npx next build                   -> exit 0
```

## 4. Regression proof for the new fact (declared deviation)

```
temporarily re-added "return.receipt.unit_quantity_invalid" to the set in the working copy
npx tsx … --test --test-name-pattern "survives a unit-metadata refusal" …
  ✖ an unknown receipt send survives a unit-metadata refusal and only the exact replay resolves it
    AssertionError at line 590 (deepEqual of the retained envelope)
    actual: null      <-- the lookup refusal erased the possibly-sent intent: exactly the Architect's defect
restored the file byte-for-byte from the pre-experiment copy; git diff shows only the intended 12+/4- hunk
re-ran the full mounted suite -> 9/9 pass
```

Nothing from the experiment was committed.

## 5. Served artifacts

```
Stop-Process 7864 (old `next start -p 3000`)
npx next build      -> BUILD_ID GptasB03cazPtYoJp7aiE   (previous 0RrDo7CGZ60FPsYrWxQ1a)
node node_modules/next/dist/bin/next start -p 3000   -> PID 23760
GET /fa                                                    -> 200
GET /_next/static/GptasB03cazPtYoJp7aiE/_buildManifest.js   -> 200
GET /fa/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490  -> 200 (19,317 B)
GET /_next/static/chunks/7952-1ecaa58d415c5e34.js
    set literal served: 9 tokens, ending "return.receipt.origin_invalid"
    body.includes("return.receipt.unit_quantity_invalid") -> false
GET /_next/static/chunks/1323-979bf525b6515616.js
    still carries the fa/en message for return.receipt.unit_quantity_invalid (operator copy preserved)
```

Host was not touched (no backend change): `5088` PID `21060`, dll SHA256
`8AA71DD82E6D737F9698A93D0F99EE7A77EB07E02916E4185DE9C271890E7948`, `/health` 200, `/health/ready` 200.

## 6. Data

```
migrations added/applied: none; DDL/backfill/seed/reset/cleanup/restore: none
live HTTP POST: 0   live receipt/decision/refund/stock/provider call: 0   new case: 0
canonical database: ONE tooba_alpha (127.0.0.1:5432, SingleStore store-alpha)
live case 01a1e6b0-… read-only (Completed, updated_at 00:31:26.384627Z, 1 item/receipt/line/decision/attempt)
```
