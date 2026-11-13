# RECEIPTS — TB-SHIPPING-AUTHORING-MOBILE-RESPONSIVE-UI-264-R3

Every fact below was read from this machine in this round. Nothing here is inferred from a test name or from the JSX.

## A. Repository identity

| # | Check | Result |
| --- | --- | --- |
| A1 | `git rev-parse HEAD` before the change | `c60458a9524c0d5fd42ea0e805e3212073f7ec41` (work base) |
| A2 | `git merge-base --is-ancestor 18c291d7eb4ebec2cb5c7b6a5c97cf2a721f11fb HEAD; $LASTEXITCODE` | `0` → expected base **is** an ancestor |
| A3 | `git rev-list --count 18c291d7…..HEAD` | `5` — all five are the docs-only issue/recovery commits (`5df02823c` + four `docs(shipping): pin exact architect continuation…`) |
| A4 | `git status --porcelain` before staging | exactly one modified file: `src/frontend/features/admin-shipping-authoring/ui/authoring-screen.tsx` |
| A5 | `git diff --stat` | `1 file changed, 14 insertions(+), 5 deletions(-)` |
| A6 | code commit | `b7cd528192832b92b4c89436a6a6e48d0e06268d` — `fix(admin-shipping-authoring): keep the mobile header identity row readable` |
| A7 | working tree after commit | clean (no stray/untracked files; no `--amend`, no force, no reset, no stash, no rebase) |

## B. Frontend checks (each once, after the final edit)

| # | Command | Result |
| --- | --- | --- |
| B1 | `npx tsc --noEmit` | 0 errors |
| B2 | `npm run build` | 0 — `BUILD_ID` `Ex36QU4MPjSKfm8VUlqDC` |
| B3 | authoring test files (33 files) | **274 passed**, 0 failed |
| B4 | `sales-ready-ui-246-r1.test.ts` + `-r2.test.ts` | **13/13** |
| B5 | `authoring-carrier-calendar.mount.test.ts` + `tb261-notification.mount.test.ts` | **7/7** — one Toast per authoritative action, MessageBox only for a blocking decision, notification wiring unchanged |

## C. Served runtime identity

| # | Probe | Result |
| --- | --- | --- |
| C1 | `GET http://127.0.0.1:3000/fa` | `200` |
| C2 | `GET /_next/static/Ex36QU4MPjSKfm8VUlqDC/_buildManifest.js` | `200` — the running server serves this exact build |
| C3 | `GET /_next/static/chunks/6138-3bc417f144f96c81.js` | `200`; the file contains `shipping-authoring-page-header` ×1, `lg:sticky` ×1, `md:w-auto` ×2, `shipping-authoring-method-name` ×1, `shipping-authoring-toolbar` ×1 → the served chunk is the corrected code |
| C4 | listener on `:3000` | PID **37012** |
| C5 | listener on `:5088` | PID **49024**, `GET /health` → `200` (backend untouched, not restarted) |
| C6 | `.next/BUILD_ID` | `Ex36QU4MPjSKfm8VUlqDC` |

## D. Emitted-CSS constraint

| # | Probe | Result |
| --- | --- | --- |
| D1 | `tailwind.config.ts` `content` | `["./app/**/*.{ts,tsx}", "./design-system/**/*.{ts,tsx}", "./lib/**/*.{ts,tsx}"]` — **`features/**` is absent** |
| D2 | regex count in served `69e1de4d01ca9cf4.css` (129,091 bytes) | `\.w-full\{` 1, `\.grow\{` 1, `\.md\\:w-auto\{` 1, `\.lg\\:sticky\{` 1, `\.truncate\{` 1, `\.min-w-0\{` 1, `\.shrink-0\{` 1, `\.leading-6\{` 1, `\.min-h-11\{` 1 |

Consequence recorded in `RESULT.md` §4: only classes that already exist outside `features/**` can be trusted in a feature file, so the correction reuses emitted utilities and the geometry below was confirmed in the DOM rather than assumed from the class names.

## E. Read-only geometry — before / after on the same served page

Harness: `dom/harness/cdp-before-after.mjs` → `dom/tb264r3-before-after.json`. Headless Chrome, task-owned profile `D:\temp\tb264r3-chrome`, authenticated session already present, `GET` only. "before" is produced by re-applying the pre-fix class names **in the live DOM** (no source change, no reload of a different build, no save).

Route: `/fa/admin/shipping-services/authoring/01a1cd43-b98d-7000-b2a9-a35ab39d8058`
Draft id is the labelled task-owned draft; the user's `01a17d0d-…` was never opened for writing.

| width | state | identity | title | method | header | position | doc overflow |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 360 | before | 65px | 65px / 3 lines (72px) | clipped | 222px | `sticky` | 0 |
| 360 | **after** | **255px** | **151px / 1 line** | **not clipped** | 194px | `static` | 0 |
| 390 | before | 95px | 95px / 2 lines (48px) | clipped | 198px | `sticky` | 0 |
| 390 | **after** | **285px** | **151px / 1 line** | **not clipped** | 194px | `static` | 0 |

Also in the same run: buttons/links under 44px = **0** at both mobile widths; the only sub-90px fields are the seven 16×16 day radios (`radio:16x16` ×7), which also exist at 1440.

## F. Read-only geometry — the whole form at three widths

Harness: `dom/harness/cdp-geometry.mjs` → `dom/tb264r3-geometry.json`.

| fact | 360 | 390 | 1440 |
| --- | --- | --- | --- |
| `documentElement.clientWidth` | 345 | 375 | 1425 |
| authoring content width | 281px | 311px | 1024px |
| header | 281×194, `static` | 311×194, `static` | 1024×62, **`sticky`** |
| identity | 255×72 | 285×72 | 622×44 |
| title | 151×24, **1 line** | 151×24, **1 line** | 151×24, 1 line |
| status badge | 80×44 «منتشرشده» | 80×44 | 80×44 |
| method | 255px wide, `clipped:false`, `title` attr present | 285px, not clipped | 245px inline, not clipped |
| toolbar | 178×44, `collidesWithHeader:false` | 178×44, false | 178×44, false |
| header buttons | back 182×44, `ویرایش` 68×44, `ذخیره تغییرات` 102×44 | same | same |
| doc / body scrollWidth | 345 / 345 (**0 overflow**) | 375 / 375 (**0**) | 1425 / 1425 (**0**) |
| off-screen controls | 0 | 0 | 0 |
| tabs | 2 × 44px, active «اطلاعات روش» | same | same |
| timing tabs | 3: «تحویل به مشتری», «پذیرش حامل», «آماده‌سازی انبار» | same | same |
| day rows | 7 × (253px, 32–33px), 0 overflowing, 0 collapsed | 7 × 283px | 7 × 988px |
| labels | 12 present / 5 with a resolvable `for` | same | same |
| Save | present, `disabled:true` (not dirty), no unsaved hint | same | same |

## G. Read-only geometry — every tab and timing editor at mobile widths

Harness: `dom/harness/cdp-tabs.mjs` → `dom/tb264r3-tabs.json`. Tab clicks only (no edit, no save, no publication, no discard, no mode change).

| width | step | timing tabs | day rows | row overflow | row offscreen | narrow text inputs | clipped text inputs | doc overflow | min button |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 360 | `اطلاعات روش` (customer tab default) | 44/44/44 | 7 × 253px | 0 | 0 | 0 | 0 | 0 | 44 |
| 360 | `تحویل به مشتری` | 44/44/44 | `shipping-authoring-week-day-*` ×7 | 0 | 0 | 0 | 0 | 0 | 44 |
| 360 | `پذیرش حامل` | 44/44/44 | `shipping-authoring-carrier-day-*` ×7 | 0 | 0 | 0 | 0 | 0 | 44 |
| 360 | `آماده‌سازی انبار` | 44/44/44 | `shipping-authoring-processing-day-*` ×7 | 0 | 0 | 0 | 0 | 0 | 44 |
| 360 | `مناطق و تعرفه‌ها` | — | — | — | 0 | — | — | 0 | 44 |
| 390 | same four steps | 44/44/44 | 7 × 283px | 0 | 0 | 0 | 0 | 0 | 44 |
| 390 | `مناطق و تعرفه‌ها` | — | — | — | 0 | — | — | 0 | 44 |

## H. What was NOT done (audit of side effects)

- No backend change, no Host rebuild or restart, no migration, no schema/API/locale-registry change, no scheduling/authority/capture/queue/selection code touched.
- No save, no publication, no withdraw, no discard write, no mode change, no data mutation of any kind, on the task draft or anywhere else.
- The user's service draft `01a17d0d-e033-7000-af43-49a1daa28822` was not opened for writing, not saved, not published, not rolled back and not re-audited.
- No new/disposable/test/parallel/Docker database, no 15433, no edition/connection/port/appsettings change, no reset/reseed/backfill/manual DDL/business SQL/global reclaimer.
- DataProtection and CapturedPreview unchanged (local ON / checked-in OFF). `255` parked, live Tipax deferred, no provider call, no fake waybill.
- No `--amend`, no force-push, no reset, no stash, no rebase, no unrelated file overwritten.
- The three harness scripts and their JSON output were added under `dom/` for reproducibility; the scripts are read-only (`GET` + DOM read, with the single documented live-DOM class swap used only to record the "before" geometry on the same page).
