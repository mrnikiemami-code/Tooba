# TB-268 — RECEIPTS

All commands were run once, in order, after the final source edit. Nothing here is a repeated green loop and
nothing here writes business data.

## 1 Typecheck

```
$ cd src/frontend && npx tsc --noEmit
TSC_EXIT=0
```

## 2 Mounted suite (the existing harness, not a new one)

```
$ cd src/frontend
$ npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
ℹ tests 70
ℹ suites 0
ℹ pass 70
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 5291.2367
```

The seven cases this task added:

```
✔ TB-268: the strip shows «همه» plus only the non-zero categories, in Persian-digit counts
✔ TB-268: overlapping lineage counts on both tabs, and a resend is not a status
✔ TB-268: RTL keyboard navigation moves the selection and the focus together
✔ TB-268: a lineage chip reveals the EXISTING counterpart card in its own tab
✔ TB-268: one parcel, or a not-yet-authoritative projection, adds no navigation
✔ TB-268: a retained unresolved attempt funds «نیازمند اقدام» and survives a tab switch
✔ TB-268: a selected tab that becomes empty keeps its selection and says so
```

Note for the architect: the *earlier* failures I hit in this round came from launching the suite with plain
`npx vitest`, which is **not** this repository's mounted-test harness (`tsx --test` with
`tsconfig.mount-tests.json`). The vitest attempt was discarded; it is not evidence of a source problem and the
file was never corrupted (a `git stash` round-trip re-hashed to the same bytes: `631D5DB3…`, `A0FDE476…`,
`32292E5B…`).

## 3 Production build

```
$ cd src/frontend && npm run build
✓ Compiled successfully in 9.2s
BUILD_ID vJWB2FG1oJ2-vHasmjtve
```

## 4 Served build (the running process serves what was built)

```
$ Get-Content .next/BUILD_ID
vJWB2FG1oJ2-vHasmjtve

GET http://127.0.0.1:3000/_next/static/vJWB2FG1oJ2-vHasmjtve/_buildManifest.js   -> 200
GET http://127.0.0.1:3000/_next/static/chunks/7825-41be5c01279d084e.js          -> 200
GET http://127.0.0.1:3000/_next/static/chunks/967-9c7248c75c25d481.js           -> 200
```

The served bytes carry the new copy (`7825-41be5c01279d084e.js`):
`نیازمند اقدام` ×1, `مرسوله‌ای در این بخش نیست` ×1, `دریافت‌شده در مبدأ` ×2.

Restart: the listener on 3000 (old PID 28360) was stopped and `npx next start -p 3000` was started again;
the new listener is **PID 32300**, `GET /fa` → **200**.

## 5 Live read-only measurements on Order B

Same URL for every viewport; the page was only read.

```
390  (fresh load)  document 390/390   strip 502/306 scrollable   tabs 44px   selected visible 42..169 in 42..348   card 306
1024               document 1009/1009 strip 606/606 fits         tabs 44px
1440               document 1425/1425 strip 1022/1022 fits       tabs 44px   card 1037 in main 1169
```

Counts read from the served DOM at 1440:

```
همه (۲) · دریافت‌شده در مبدأ (۱) · ارسال مجدد (۱) · تحویل‌شده (۱)     [aria-selected=true on ارسال مجدد]
panel: مرسولهٔ ۲ | ارسال مجدد از مرسولهٔ ۱ | تیپاکس · ۱ قلم | تحویل‌شده | کد رهگیری TRK-01A1DC16482D70008D6453B199873504 | کپی
```

Keyboard, live:

```
focus selected tab «ارسال مجدد»            -> document.activeElement === that tab
press ArrowLeft (DOM keyboard event)       -> aria-selected: تحویل‌شده=true, focus on «تحویل‌شده», its tabIndex 0, others -1
```

Cross-tab lineage, live:

```
click chip «ارسال مجدد از مرسولهٔ ۱»
-> tabs: دریافت‌شده در مبدأ = true
-> panel card admin-order-operational-parcel-01a1dc15-b0b7-7000-9118-953a3f6e92d2 focused = true
-> document 390/390 (no overflow)
```

Touch target of the inline chip:

```
chip box 24px ; ::before computed height 44px (-10px top, -10px bottom)
=> the only sub-44 box in `main` is a 44px touch target
```

## 6 Architecture guards (baseline comparison, not a new gate)

```
$ cd src/frontend && npm run test:architecture          # with this task's changes
ℹ tests 11   pass 8   fail 3

$ git stash push -- <the three changed files> && npm run test:architecture
ℹ tests 11   pass 8   fail 3        # identical failures: FE-FOLDER-001, FE-FOLDER-002, FE-SIZE baseline drift
$ git stash pop
A0FDE476… / 631D5DB3… / 32292E5B…    # file hashes restored byte-for-byte
```

So the three guard failures are pre-existing task-wide baselines (`admin-api.ts` 1399 > 1002,
`admin-error-map.ts` 1348 > 1002, the flat `app/admin/*` business files, the `admin-api` export dumping ground),
**not** introduced by TB-268. Recorded here instead of being quietly "fixed" inside a presentation-only task.

## 7 Runtime / data

```
Host 5088   /health -> 200   /health/ready -> 200      (untouched by this task)
FE 3000     /fa -> 200, BUILD_ID vJWB2FG1oJ2-vHasmjtve, PID 32300
database    ONE tooba_alpha @ 127.0.0.1:5432, SingleStore, tenant store-alpha
migration   none added, none applied
data write  none (no DDL, no business SQL, no reset, no reseed, no cleanup)
user order  01a1db2f-e36e-7000-b7d3-afb2fe86e81a read-only (GET only) — source still Returned, resend still Delivered
```

## 8 Working tree

```
M src/frontend/app/admin/admin-order-items-shipping-panel.tsx               (+1)
M src/frontend/app/admin/admin-order-operational-delivery-section.tsx       (+427 / -26)
M src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts     (+369 / -9)
A docs/architecture/evidence/TB-SHIPPING-PARCEL-TABS-AND-QUIET-OPERATOR-VIEW-268/ (this folder)
```

A stray `post-dispatch-attempt-store.ts.bak` left in the tree during this round was deleted before the commit;
the tracked source of that store was never modified.
Untracked prior-run scratch scripts `scripts/tb266r1-*.mjs` remain and are not part of this change set.
