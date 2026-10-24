# TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205 — RESULT (PARTIAL)

Base-SHA: `b7f7ead6df5129a9b0a62ee201592c4a2888f1ae`
Status: **PARTIAL** — mounted-route + synthetic-interception readiness proven; the A–E interaction
scenarios and the FA/EN × desktop/mobile visual review were **NOT executed** inside the 30-minute
timebox. Nothing here is production-functional, database, or visual acceptance.

## 1. Runtime actually used (setup finished inside the 5-minute allowance)

| Item | Observed |
| --- | --- |
| Dev runtime | one task-owned FRONTEND-ONLY `next dev` on `127.0.0.1:3517` |
| Source | task-owned tracked-source copy `D:\temp\tb205-runtime\frontend` (robocopy of `src/frontend`, excluding `node_modules`, `.next`, logs); `node_modules` is a **junction** to the real one — no install, no download |
| Shared state | shared `.next`, `tsconfig`, `next-env` untouched; the user's pre-existing dev server on `:3000` was not stopped, restarted or reconfigured |
| Env | copy-local `.env.local` replaced with exactly `TOOBA_HOST_ORIGIN=http://127.0.0.1:1` (fail-closed); no credentials, no host/DB/runner |
| Ready | `✓ Ready in 1808ms`; port `3517` proven free before start and after cleanup |
| Browser | installed `Chrome` (`C:\Program Files\Google\Chrome\Application\chrome.exe`), `--headless=new`, task-owned profile `D:\temp\tb205-runtime\chrome-profile`, CDP on `:9555`; no package/browser download, no user profile touched |

One harness correction was needed and used (the single allowed correction): the PowerShell-written
command file carried a UTF-8 BOM, so the very first command loop iteration never parsed; the harness now
strips BOM and discards undeserializable commands. The initial `Fetch.enable` pattern was also narrowed
from `*` to `*/v1/*` so document navigation is not blocked by interception.

## 2. Interception boundary (synthetic only, fail-closed)

`Fetch.enable` at `Request` stage with URL pattern `*/v1/*`, installed **before** the page was opened.
Whitelist fulfilled: `editor-document` (GET), `geography/countries`, `geography/.../administrative-areas`,
`geography/.../cities`, `geography/cities/.../service-areas`. Every other `/v1` call is
`Fetch.failRequest` locally — it never reaches the dev server, the rewrite target, or any backend.

Fixtures follow the **current** wire contract read from source, not from memory:
`editor-document-decode.ts` (GUID/time/decimal/variant strictness, 7 unique weekdays, exactly one
default language, `destinationFacts` proven against the selected tree) and `geography-lookup-api.ts`
(`{id, code, label, parentId, isActive}` with parent evidence). Money is emitted as **raw number tokens**
`0.50` / `1850000.00` (the fixture documents are template literals, never `JSON.stringify`, so trailing
zeros survive). Week day 1 carries `09:00:00.500` with peer `17:30:00`; the fixture set includes a
non-null-admin city and a null-admin city, an inactive country and an inactive area, two groups with
retained ids, mixed fa/EN content (fa default, rtl) and a group title present only in fa.

## 3. What was actually observed on the mounted route

Route was the real `/admin/shipping-services/authoring/<synthetic-guid>?mode=edit`, rendered by the real
`ShippingAuthoringScreen` in the real browser — **not** a pure-function harness.

Network observed for that navigation (exact counts from the interception log):

- `GET .../editor-document` — **2** (React strict-mode double read, both fulfilled with the synthetic document)
- `GET /v1/admin/geography/countries?languageId=<fa>` — **4** (fulfilled; fixture returns an inactive second country on purpose)
- `GET /v1/admin/dev-context`, `GET /v1/admin/access-control/me/capabilities`,
  `GET /v1/admin/shipping-services/authoring-types` — **6** total, all `BLOCKED_LOCAL`
- `PUT` — **0**. No write of any kind was attempted, so no merchant mutation could occur.

Rendered DOM (evidence `screenshots/01-view-fa-1440.png`, fa/RTL, 1440×900) shows the real editor shell:
owner line "مالک ارسال: همین فروشگاه", type section with the accepted "بدون نوع (پاک‌کردن صریح)" reset,
the two language tabs فارسی/انگلیسی with "زبان پیش‌فرض" markers, "۲ از ۲ زبان ترجمه شده", the freight
payment and calculation-mode controls, `انصراف از ویرایش` and `اعمال`, plus the two-part
`اطلاعات روش` / `مناطق و هزینه‌ها` tab structure and the view/edit banner.

**Fixture-boundary finding, not yet classified as a product defect:** the page shows the banner
`بررسی پیش‌نویس ناموفق بود.` The three blocked `/v1` reads above belong to the surrounding shell
(dev-context, capabilities, authoring-types); because the whitelist failed them locally by design, the
banner is the expected fail-closed reaction of the screen to unavailable shell reads. Confirming that
(it versus a real editor defect) requires fulfilling those three envelopes, which was outside the
remaining time. No production change was made and no defect is claimed.

## 4. Scenarios NOT executed (honest gap list)

All of A–E remain unexecuted: General load/edit/language-switch/blank-name-and-discard; region add,
geography traversal through real options, chip add/remove, nested `0.50` row, retained ids; root-week
single fractional start edit with intact end token; the composed ONE-PUT Apply with real synthetic
server ids/revision and the pending-double-click count; and the scripted 409 / transport-failure
outcome-unknown reconcile path. The mounted page and the interception boundary are ready to run them;
the 30-minute box was consumed by runtime bring-up (isolated copy, fail-closed env, headless CDP
harness, BOM defect) and expired before the interaction phase began. Per the task's stop rule this is
reported as PARTIAL rather than stretched.

## 5. Visual review

NOT EXECUTED. One synthetic screenshot exists as mount evidence only. No FA-vs-EN or desktop-vs-mobile
comparison against the accepted `186-R2` reference was performed, no spacing/overflow/label/direction
conclusion is asserted, and no design-system/CSS file was touched. `Visual: REVIEW_EVIDENCE_ONLY` for
the single capture; a screenshot is not user Visual ACCEPT.

## 6. Limits and cleanup

No production file changed: no page/component/model/api/route, no backend/Contracts/DTO/Host/DB/schema/
flag, no package/config/tsconfig/baseline/skill/AGENTS change, no `typecheck`-as-visual claim, no
whole-suite loop. New content is task-owned only: the task receipt, this evidence directory
(`scripts/fixtures.mjs`, `scripts/cdp-harness.mjs`, one screenshot), this RESULT and one recovery
pointer.

Task-owned identities removed on exit: harness `node` (PID 13332), the `next dev :3517` tree
(cmd → npx → next → start-server) and headless Chrome under `chrome-profile`. Verified afterwards:
port `3517` free, port `9555` free; the pre-existing `:3000` dev server untouched. Temporary tree
`D:\temp\tb205-runtime` is not committed.

Shared-Database-Data-Flag-Changes: NONE
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
