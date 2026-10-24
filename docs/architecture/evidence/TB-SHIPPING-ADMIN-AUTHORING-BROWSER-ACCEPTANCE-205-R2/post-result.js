/* Posts the TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2 worker result to Bridge (complete contract only). */
const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_PREVALIDATED_COMPOSITE_SAVE_AND_LOCALE_BROWSER_PROOF
Bridge-Task-Row: 7973bbcd-8e6f-48f1-8999-4782a1867276
Status: PASS
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 0c8f2803462442b8eda8aa51823420f196d7519f
Base-SHA: 0c8f2803462442b8eda8aa51823420f196d7519f
Code-SHA: 4dddb7265e98a5ec012d11be0a7d2b7b982f982e
Evidence-SHA: 4dddb7265e98a5ec012d11be0a7d2b7b982f982e
HEAD-Origin-Parity: HEAD == origin/main == 4dddb7265e98a5ec012d11be0a7d2b7b982f982e (fetch verified)
Mounted-Route: YES (/admin/shipping-services/authoring/<IDS.draft>?mode=edit, real ShippingAuthoringScreen)
Scenario-Request-Assertion: PASS (exact prepared expectedRequestText match, no fabricated success)
Pending-Duplicate-Gate: PASS (putCount stays 1)
Save-Adoption: PASS (view mode, revision8, both edited names, Monday end 18:15 with start 09:00, new group/title/row and assigned ids)
Post-Save-GET: NONE (editor GET count unchanged at 2)
Locale-Captures: 6 (FA/RTL and EN/LTR, 1440x900 and 390x844, real tooba_locale cookie + Page.reload)
Not-Run-Controls: blank-name/discard, scripted409, transport-failure/outcomeUnknown reconcile, full geography matrix, group move/remove
Production-Functional-Acceptance: PENDING
Shared-Database-Data-Flag-Changes: NONE
Visual: REVIEW_EVIDENCE_ONLY
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE

1. PREPARED SCENARIO REUSED, NOT REDESIGNED
docs/.../205-R2/scripts/save-scenario.mjs was the only fixture source: initialResponseText, savedResponseText,
expectedRequestText, assertScenarioRequest and preflight were imported unchanged. Exactly ONE preflight
invocation was made and passed: {"ok":true,"result":{"initialRevision":7,"savedRevision":8,
"newGroupId":"77777777-7777-4777-8777-999999999999","newRowId":"88888888-8888-4888-8888-999999999999",
"validReadResponses":2,"invalidWriteAsReadRejected":true}}. Harness wiring only: editor GET returns
initialResponseText before the save and savedResponseText after it; the one PUT is captured, its body is
validated by assertScenarioRequest, and the prepared response is returned ONLY when that validation passes
(otherwise the harness answers 400 rather than faking success). One hold/release of the single paused PUT
allowed the pending duplicate click to be measured. R1 fixtures still serve countries/types/shell; the only
branch correction is country2 (Germany) returning a successful EMPTY areas/cities pair because the R1 area
rows belong to country1. All other /v1 traffic fails locally; no real mutation.

2. RUNTIME
Task-owned frontend-only next dev on 127.0.0.1:3517 from D:\\temp\\tb205-runtime\\frontend (node_modules
junction; .env.local = TOOBA_HOST_ORIGIN=http://127.0.0.1:1). Ports 3517/9555 proven free before start.
Installed Chrome --headless=new, fresh task profile, CDP :9555, interception installed BEFORE navigation.
Setup about 3 minutes; server Ready in 1904ms.

3. EXECUTED SCENARIO (real clicks/typing on production components)
FA name -> "ارسال استاندارد R2"; switched to the EN CONTENT tab -> "Standard shipping R2"; back to FA and the
FA value was retained. Monday (ISO1) END only -> 18:15 with start untouched. Regions: added one group with FA
title "گروه آزمایشی"; in that group's real picker selected country آلمان (…1111-2222), added the destination
(chip کشور: آلمان); added one nested row freight 0.50, packaging 0.00, delivery 1..2 with every optional
condition blank. Retained group1 (استان: تهران) and group2 (شهر: قم) were untouched; no reorder or removal.
Then ONE Apply and the PUT was HELD:
- assertScenarioRequest PASSED: expectedRevision 7; two retained groups with their read-shape areas and full
  stored rows; the new group with groupId null / rateRowId null and its FA title patch; both edited
  translations; the entire week with Monday end 18:15:00 and start 09:00:00.500; exact raw tokens
  "freightAmount":0.50 and "packagingAmount":1850000.00.
- Pending duplicate gate: while held, the save control was already "در حال ذخیره…" and disabled; a real click
  on it created NO second request and putCount stayed 1 before and after release.
- Release returned the prepared response; the UI left the saving/unknown state, entered VIEW mode and showed
  the adopted document: FA name "ارسال استاندارد R2", three groups including "گروه ۳" with "کشور: آلمان", and
  the week "دوشنبه 09:00 تا 18:15" with every other day unchanged and exactly one root week. Revision 8 and
  the assigned ids came from the confirmed document, not from fixture text alone.
- NO post-save GET: editor GETs were 2 before Apply (StrictMode initial double read) and still 2 afterwards.

4. LOCALE/VIEWPORT CAPTURES
Interface locale was changed with the canonical tooba_locale cookie via CDP Network.setCookie plus a full
Page.reload of the same actual route - not --lang, not the content tab, not a dir attribute. DOM confirmed
lang=fa/dir=rtl before and lang=en/dir=ltr after. Six captures: 01-fa-1440-regions-edit, 02-fa-1440-after-save,
03-fa-1440-general, 04-fa-390-regions, 05-en-1440-regions, 06-en-390-general. Observed: the two-part
General/Regions structure and the single shared week render in both locales; EN/LTR flips the whole chrome
direction (Back to shipping methods, Method information, Regions and costs, Apply) while stored Persian
content stays RTL inside its own fields; each group renders its geography once, all ten rate axes are
labelled per row, and no horizontal overflow appeared at 390x844 in the captured frames. The reload served
the adopted revision-8 document from the prepared read response - fixture-boundary persistence, not a
database read.

5. EXPLICIT REMAINING GAPS (not claimed)
blank-name / incomplete-time / pending-timezone blocking, reject-discard and Cancel-Edit restoration; scripted
409 and PUT transport-failure/outcomeUnknown with explicit reconcile (only the successful path and the
pending-duplicate gate were exercised); the full geography matrix (null-admin city and country-wide non-null
parent through the picker, inactive-option disabled state), group move/remove isolation and chip removal. The
EN name is not visible in view mode because the default content language is FA; its adoption is proven by the
asserted request and the saved document, not by a separate EN-name view screenshot. This run therefore does
NOT certify all of A-E and is not user Visual ACCEPT.

6. LIMITS AND CLEANUP
No production file changed (no page/component/model/api/route/backend/Contracts/Host/schema/flag); no
package/config/baseline/skill/AGENTS change; no build/typecheck/suite sweep; no Bridge probe; no historical
post-result.js executed. New content is task-owned only: this RESULT, scripts/cdp-harness.mjs, one-shot
scripts/preflight.mjs, the six screenshots, the task receipt and one recovery pointer. ONE harness correction
was required (the result-reading helper raced the writer); the prepared scenario was never modified.
Cleanup: harness node, headless Chrome under the task profile and the task-owned next dev :3517 tree stopped;
ports 3517 and 9555 verified NOT listening afterwards and the pre-existing :3000 dev server untouched.
Claim-UTC: 2026-10-24T16:07:31Z / Result-UTC: 2026-10-24T16:33:00Z / Elapsed-Minutes: 26 (hard box 25)
END_TOOBA_WORKER_RESULT`;

const res = await fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ taskId: "TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2", channelId: "tooba-main", workerId: "tooba-worker-01", content })
});
console.log(res.status, await res.text());
