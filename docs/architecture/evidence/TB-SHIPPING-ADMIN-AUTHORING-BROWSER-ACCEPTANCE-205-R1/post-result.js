/* Posts the TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1 worker result to Bridge (complete contract only). */
const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_REUSED_BROWSER_HARNESS_INTERACTION_AND_VISUAL_COMPLETION
Bridge-Task-Row: 9b3a2c77-afce-4f9c-a1b5-bd63bb86beee
Status: PARTIAL
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: be286fcb788aee83c3fd5733221065bc470d1e15
Base-SHA: be286fcb788aee83c3fd5733221065bc470d1e15
Code-SHA: e827c5d60aabe52cc243369df6fa4a73e5f2be86
Evidence-SHA: e827c5d60aabe52cc243369df6fa4a73e5f2be86
HEAD-Origin-Parity: HEAD == origin/main == e827c5d60aabe52cc243369df6fa4a73e5f2be86 (fetch verified)
Runtime-Source-SHA: be286fcb788aee83c3fd5733221065bc470d1e15 (task-owned copy of src/frontend, no production edit)
Mounted-Route: YES (/admin/shipping-services/authoring/<synthetic-guid>?mode=edit, real ShippingAuthoringScreen)
Interaction-Scenarios: A_PARTIAL, D_PARTIAL, out-of-band outcome-unknown; B, C, E NOT_EXECUTED
Fixture-Vs-Live-Boundary: ALL /v1 synthetic, fail-closed locally; zero real merchant mutation; PUT count 1 (synthetic fulfil)
Production-Functional-Acceptance: PENDING
Shared-Database-Data-Flag-Changes: NONE
Visual: REVIEW_EVIDENCE_ONLY (5 FA screenshots; EN/LTR NOT captured)
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE

RESULT IS HONESTLY PARTIAL. All six harness/fixture defects from the 205 review were repaired and a real
mounted General + save attempt was driven with real clicks/typing, but save ADOPTION is NOT proven,
EN/LTR was not captured, and B/C/E were not executed.

1. HARNESS/FIXTURE REPAIRS (each verified in the run, not asserted)
(1) childOption now emits JSON null for a null-admin parent instead of the quoted string "null"; GUID
parents stay quoted. (2) The parent/descendant overlap is gone: group1 is the AdministrativeArea terminal
only and group2 is the null-admin City terminal only, so no retained selection contains its own
descendant; production overlap validation was NOT weakened. (3) City mocks are now query-consistent:
presence of administrativeAreaId= selects the filtered list (exactly that parent, no null-parent row),
otherwise the country-wide list (null-admin row plus an in-country non-null parent). (4) The editor PUT
response is a prepared FULL document for this scenario: the current document's weeklySchedule /
regionGroups / methodTranslations blocks are replaced by THIS request's own text via brace-balanced
splicing (so real number tokens survive) with a new revision - no generic mock engine, no revision-only
text trick. (5) authoring-types is fulfilled with the real {types:[...]} envelope of
decodeAuthoringTypeChoices. (6) Every CDP call now carries a finite timeout and returns a
timeout/protocol error that the runner reports as an error instead of silently treating it as success;
the BOM strip and */v1/*-only Fetch interception are retained. dev-context ({actorUserId} of
prepareAdminDevActor) and me/capabilities ({userId, roleCodes, permissions[]} of mapEffective) are
whitelisted with their real reader shapes; unhandled /v1 still fails locally and none occurred because
every requested envelope is now served.

2. 205 CORRECTION (append-only to the 205 RESULT, history not rewritten)
The Architect's source inspection is CONFIRMED by execution: authoring-types is FEATURE traffic consumed
by the type selector and its failure is what rendered messages.entryFailed. With the real types envelope
served the banner is gone (pre-state banner:false) and the type select renders the stored type. It was a
fixture-boundary effect, not a product defect.

3. EXECUTED ON THE MOUNTED ROUTE
A (partial): loaded with no banner; in the FA tab the name field (dir=rtl) went from "ارسال استاندارد" to
"ارسال استاندارد R1", Apply became enabled and the dirty indicator "تغییر اعمال‌نشده دارید" appeared;
switching to the EN tab moved the field to dir=ltr holding the EN value "Standard shipping", which was set
to "Standard shipping R1"; switching back to FA RETAINED the FA edit - per-language state, not one shared
field. NOT executed: blank-name issue/Apply-block, reject-discard, Cancel-Edit restoration and
re-enter-without-residue.
D (partial): Apply produced EXACTLY ONE PUT and NO post-save GET. Asserted from the captured request text:
expectedRevision=7 equals the revision the page loaded; retained group ids ...7777-1111 and ...7777-2222
and retained rate row ...8888-1111 are present; exact raw number tokens 0.50 and 1850000.00 are present;
methodTranslations carries both edited names with their own languageId (FA "ارسال استاندارد R1",
EN "Standard shipping R1"). NOT in the body: currencyCode (server-owned) and weeklySchedule - an
UNCHANGED week is omitted from the write body, so the fractional-start/peer-intact assertion could not be
exercised because the week was never edited in this run.
Outcome-unknown (observed, and correct product behaviour): the prepared response was valid JSON but not
decodable against the injected regionGroups (its destinationFacts no longer matched the selected tree), so
the editor refused to claim success and showed "نتیجهٔ ذخیره مشخص نیست... ابتدا وضعیت سرور را بخوانید"
with an explicit "خواندن وضعیت از سرور" action, kept the work dirty and sent NO second PUT. The missing
piece is my prepared response, not the product. Therefore new-revision/new-id ADOPTION IS NOT PROVEN.
NOT executed: B (group add, geography traversal through real option rows, chip add/remove, nested 0.50 row,
null-admin vs country-wide city through the picker, inactive-option disable, group move/remove isolation),
C (week edit), E (scripted 409, transport failure + explicit reconcile), and the pending-double-click count.

4. VISUAL
Five synthetic screenshots committed (all FA/RTL): 11-fa-1440-general, 12-fa-1440-regions,
13-fa-1440-after-apply, 14-fa-390-regions, 15-fa-390-general. Observed: two-part General/Regions
structure, owner line, type select, payment/calculation radios, language tabs with default markers, group
list with retained "گروه ۱"/"گروه ۲", geography selectors, "افزودن این مقصد", rate rows and one root
week all present and RTL at 1440x900; at 390x844 the layout reflows to one column with no horizontal
overflow in the captured frame and the tab strip reachable. EN/LTR was NOT captured: the interface locale
is cookie-driven (tooba_locale) and the page stayed dir=rtl/lang=fa. No design-system/CSS change. These are
review evidence, NOT user Visual ACCEPT.

5. LIMITS, CORRECTIONS, CLEANUP
No production file changed: no page/component/model/api/route, no backend/Contracts/DTO/Host/schema/flag,
no package/config/tsconfig/baseline/skill/AGENTS change, no typecheck-as-visual claim, no broad test sweep.
Two harness/fixture correction iterations were used (the allowed maximum); the repeated multi-instance
diagnostic churn was my own process hygiene and is disclosed rather than hidden. Task-owned
frontend-only next dev on 127.0.0.1:3517 (copy D:\\temp\\tb205-runtime\\frontend, node_modules junction,
.env.local = TOOBA_HOST_ORIGIN=http://127.0.0.1:1) and installed Chrome --headless=new with CDP on :9555;
no install/download, no shared .next/tsconfig/next-env mutation. Cleanup: all harness node processes,
headless Chrome under the task profile and the task-owned next dev tree stopped; ports 3517 and 9555
verified free afterwards and the pre-existing :3000 dev server untouched.
Claim-UTC: 2026-10-24T15:47:00Z (Working heartbeat) / Result-UTC: 2026-10-24T16:10:00Z
END_TOOBA_WORKER_RESULT`;

const res = await fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ taskId: "TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1", channelId: "tooba-main", workerId: "tooba-worker-01", content })
});
console.log(res.status, await res.text());
