/* Posts the TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205 worker result to Bridge (complete contract only). */
const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_REAL_ROUTE_MOUNTED_BROWSER_AND_VISUAL_REVIEW
Status: PARTIAL
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: b7f7ead6df5129a9b0a62ee201592c4a2888f1ae
Base-SHA: b7f7ead6df5129a9b0a62ee201592c4a2888f1ae
Mounted-Route: YES (/admin/shipping-services/authoring/<synthetic-guid>?mode=edit)
Interaction-Scenarios-A-B-C-D-E: NOT_EXECUTED
Production-Functional-Acceptance: PENDING
Shared-Database-Data-Flag-Changes: NONE
Visual: REVIEW_EVIDENCE_ONLY (one mount screenshot; no locale/viewport comparison completed)
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE

RESULT IS HONESTLY PARTIAL. The mounted route and the fail-closed synthetic interception boundary were
proven in a real installed browser; the A-E interaction scenarios and the FA/EN desktop/mobile visual
review were NOT executed. The whole 30-minute box went to runtime bring-up plus one harness defect.

1. RUNTIME ACTUALLY USED (setup inside the 5-minute allowance)
One task-owned FRONTEND-ONLY next dev on 127.0.0.1:3517 from a task-owned tracked-source copy
(D:\\temp\\tb205-runtime\\frontend) with node_modules as a JUNCTION to the real tree: no install, no
download, no shared .next/tsconfig/next-env mutation, and the pre-existing :3000 dev server was never
stopped, restarted or reconfigured. The copy-local .env.local was replaced with exactly
TOOBA_HOST_ORIGIN=http://127.0.0.1:1 (fail-closed); no credentials, no host, no DB, no backend runner.
Server reported Ready in 1808ms. Browser: installed Chrome (C:\\Program Files\\Google\\Chrome\\Application\\
chrome.exe) in --headless=new with a task-owned profile and CDP on :9555; no browser/package download and
no user profile touched. Port 3517 was proven free before start and free again after cleanup, and 9555
free after exit.
One harness correction (the single allowed one) was required and used: the PowerShell-written command
file carried a UTF-8 BOM, so the first command-loop iteration never deserialized; the harness now strips
BOM and discards undeserializable commands. Fetch.enable was also narrowed from * to */v1/* so document
navigation is not blocked by interception.

2. SYNTHETIC INTERCEPTION BOUNDARY (fail-closed, no merchant write)
Fetch.enable at Request stage with URL pattern */v1/* was installed BEFORE the page was opened. Whitelist
fulfilled: editor-document (GET), geography/countries, geography/.../administrative-areas,
geography/.../cities, geography/cities/.../service-areas. Every other /v1 call is Fetch.failRequest
locally and never reaches the dev server, the rewrite target or any backend. Fixtures were derived from
the CURRENT source contracts (editor-document-decode.ts strictness: GUID/time/decimal/variant, 7 unique
weekdays, exactly one default language, destinationFacts proven against the selected tree; and
geography-lookup-api.ts {id,code,label,parentId,isActive} with parent evidence). Money is emitted as raw
number tokens 0.50 / 1850000.00 because the fixture documents are template literals (never
JSON.stringify), so trailing zeros survive; week day 1 is 09:00:00.500 with peer 17:30:00; the set
includes a non-null-admin city, a null-admin city, an inactive country, an inactive area, two retained
groups, fa-default rtl plus EN content and a group title present only in fa. No production endpoint, no
flag, no page override, no fake permission or client-owner authority, no login/credential probe.

3. WHAT WAS ACTUALLY OBSERVED ON THE MOUNTED ROUTE
The real ShippingAuthoringScreen rendered the real route in the real browser. Interception counts for
that navigation: editor-document GET = 2 (strict-mode double read, both fulfilled); geography countries
GET = 4 (fulfilled, fixture deliberately returns an inactive second country); PUT = 0 (no write of any
kind was attempted, so no merchant mutation could occur); blocked locally = 6, namely
/v1/admin/dev-context, /v1/admin/access-control/me/capabilities and
/v1/admin/shipping-services/authoring-types.
Rendered fa/RTL 1440x900 shell (evidence screenshot) shows the owner line, the type section with the
"بدون نوع (پاک‌کردن صریح)" reset, the two language tabs فارسی/انگلیسی with default markers,
"2 از 2 زبان ترجمه شده", the freight-payment and calculation-mode controls, انصراف از ویرایش / اعمال, and
the two-part اطلاعات روش / مناطق و هزینه‌ها structure with the edit banner.
FIXTURE-BOUNDARY FINDING, NOT A CLAIMED PRODUCT DEFECT: the page shows "بررسی پیش‌نویس ناموفق بود." The
three blocked reads belong to the surrounding shell, so that banner is the expected fail-closed reaction
to unavailable shell reads. Distinguishing it from a real editor defect requires fulfilling those three
envelopes, which did not fit the remaining box. No production change was made and no defect is asserted.

4. NOT EXECUTED (explicit gap list)
All of A-E: General load/edit/language switch/blank-name issue and Apply-block plus dirty-close and
discard-then-reenter; region add, real geography traversal, chip add/remove, nested 0.50 row, retained
ids, country-wide vs null-admin parent evidence, inactive-option disable, group move/remove isolation;
single fractional root-week start edit with intact end token and blocked incomplete/pending timezone;
the composed ONE-PUT Apply with real synthetic new ids/revision, pending double-click single request and
no post-save GET; and the scripted 409 and transport-failure/outcomeUnknown reconcile path. The mounted
page and interception are ready to run them; the timebox expired before the interaction phase began, and
per the stop rule this was reported as PARTIAL instead of stretched.

5. VISUAL
NOT EXECUTED as review. One synthetic screenshot exists as mount evidence only; no FA-vs-EN or
desktop-vs-mobile comparison against the accepted 186-R2 reference was performed and no
spacing/overflow/label/direction conclusion is asserted. No design-system/CSS change. A screenshot is not
user Visual ACCEPT.

6. LIMITS AND CLEANUP
No production file changed: no page/component/model/api/route, no backend/Contracts/DTO/Host/DB/schema/
flag, no package/config/tsconfig/baseline/skill/AGENTS change, no typecheck-as-visual claim, no
whole-suite loop. New content is task-owned only: task receipt, the evidence directory
(scripts/fixtures.mjs, scripts/cdp-harness.mjs, one screenshot), this RESULT, and one recovery pointer.
Task-owned identities removed on exit: harness node (PID 13332), the next dev :3517 tree and headless
Chrome under chrome-profile; ports 3517 and 9555 verified free afterwards and the pre-existing :3000
dev server untouched. The temporary tree D:\\temp\\tb205-runtime is not committed.

Parity: local commit pushed to origin/main, then fetch; HEAD == origin/main.
END_TOOBA_WORKER_RESULT`;

const res = await fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ taskId: "TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205", channelId: "tooba-main", workerId: "tooba-worker-01", content })
});
console.log(res.status, await res.text());
