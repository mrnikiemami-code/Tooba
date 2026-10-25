/* Posts the TB200 worker result to the Bridge results API (complete contract only). */
const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200
Parent-Task: TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199-R1
Mode: BOUNDED_FRONTEND_TYPED_EDITOR_ADAPTER_AND_SESSION_STATE
Base-SHA: ccca74258fb70c3eb64be5c743b9ee2d93235e5a
Code-SHA: 44d74284874964d24929a426094d2264c8faefd8
Evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200/RESULT.md
Parity: HEAD == origin/main == 44d74284874964d24929a426094d2264c8faefd8
Stop-State: STOP_FOR_ARCHITECT
Production-Functional-Acceptance: PENDING
Schema-Data-Flag-Changes: NONE
Automatic-Next-Task: NONE
automaticNextImplementationTask: NONE

OWNERSHIP / PUBLIC INTERFACE
One new frontend owner src/frontend/features/admin-shipping-authoring/ with a public index.ts only:
api/wire-json.ts (strict JSON read/write, exact number text, duplicate-key rejection),
api/editor-document-decode.ts (fail-closed composite decode), api/editor-document-request.ts (ONE PUT
body builder), api/editor-document-api.ts (GET/PUT + typed failures), model/editor-document.ts (typed
read model), model/editor-edits.ts (local intent + resolved-ancestry port), session/editor-session.ts
(reducer/selectors). Transport, model and session stay separate; no page/screen is exported yet.
Reused shared lib/admin/admin-result.ts (adminRequestHeaders + dev-actor header) and the existing
same-origin /v1 rewrite; no second auth client, no page-owned app/admin import, no new transport/store
framework, no duplicate rate-policy API.

EXACT ENDPOINTS / WIRE MAPPING
GET|PUT /v1/admin/shipping-services/authoring-drafts/{draftId:guid}/editor-document via
editorDocumentPath(). Authorities traced: ShippingAuthoringEditorDocumentContracts.cs,
ShippingAuthoringEditorDocumentModels.cs, GetShippingAuthoringEditorDocumentQuery.cs,
ShippingAuthoringDraftView.cs, ShippingAuthoringTypeQueries.cs, ShippingAuthoringContextView.cs plus
Host JSON policy (camelCase, JsonStringEnumConverter, WhenWritingNull). Enums matched to their real
wire strings and unknown variants rejected (no fabricated Manual/Prepaid/Active). Decimal amounts and
TimeOnly keep their exact token text (0.50 stays 0.50, 1850000.00 stays 1850000.00) and decimal is
re-emitted as an unquoted number token without toFixed/parseInt. Required collections must be present;
optional server-null sections decode to null. Raw stored translations (including inactive language ids)
are retained untrimmed. Payload draft id must equal the requested draft id.

TYPED ADAPTER / SESSION BEHAVIOUR
One Apply = exactly one PUT with one expectedRevision (no sequence/Promise.all). Absent weekly-schedule/
settings/type wrapper = PRESERVE; supplied full object = replace; supplied {shippingTypeId:null} is an
explicit CLEAR that stays in the body; [] = no text change. Only intentionally touched active-language
name/title patches are serialized; untouched/inactive text is never echoed. Retained children keep
server ids; new children carry client-local keys that are never serialized as ids. Session state covers
load/loaded/denied/notfound/error, baseline vs local intent, saving, validationFailed, conflict and
outcomeUnknown; a successful save adopts the validated response ids/revision (same-revision no-op
accepted), failures preserve edits, duplicate Apply is gated, and results are scoped by draft id +
generation + request id so a late A load/save cannot overwrite B. Dirty close/switch raises an
explicit discard decision; nothing is silently discarded; no window.confirm/localStorage in the model.

GEOGRAPHY BOUNDARY
Read areas are (kind,id) only. The body builder requires an AreaAncestryResolver supplied later from
the existing authoritative Geography resolution path; an unresolved selection returns
{status:"needsResolution", pending:[{groupId, area}]} and NO body, so a read selection can never be
echoed into a destructive PUT or silently dropped. No new Geography backend seam/persistence/scan.

PERMISSIONS / SERVER AUTHORITY
No owner/tenant/seller/edition/currency/browser identity authority exists in the adapter, model, body
or headers; the server remains the only source of owner, budget and validation authority. No seller
selection mechanism was invented.

SAFETY OF ONE PUT / RETRY / LATE RESPONSES
Definitive 409 is a conflict (property errors kept), GET network/abort is never a fabricated 404, and a
PUT network/abort/malformed-2xx becomes outcomeUnknown with ONE attempt and no automatic replay with a
new revision. No success is derived from dispatch; only a validated server response is adopted.

COMMANDS / COUNTS
node --experimental-strip-types --test (adapter + session tests) => 13 passed, 0 failed, run once after
final correction. npx tsc --noEmit => exit 0 (after one bounded correction of a narrowing error and one
fixture decimal-encoding correction). Architecture guards (source-size, import-boundary, flat-folder,
feature-boundary, test-discovery): 7 passed, 1 failed. The failure is pre-existing unrelated baseline
growth in four files this task never touched (app/admin/category-admin-screen.tsx 2230>2215,
app/admin/landing-pages/admin-landing-page-composer.tsx 1442>1440,
app/storefront/storefront-landing-blocks.tsx 1033>1026,
lib/storefront-composition/shared-composition-renderer.tsx 918>907); no baseline/config was changed and
no new feature file is oversized. Not fixed: out of scope.
Not run: frontend build, whole discovery suite, Next server, browser/HTTP probe, backend build/test/DB/
container/migration, live network probing.

API / VISUAL IMPACT
Additive feature module only. No rendered page, route, live flat shipping screen, preview, design-system,
shared CSS, package/config, backend, schema, data or flag change. No visual acceptance or live-registry
claim is made.

CORRECTIONS (>1 correction is recorded as process deviation)
1. Fixtures encoded decimal values as JSON strings; corrected to real number tokens.
2. Duplicate-key negative fixture is rejected by the wire reader itself; test now treats null parse as
   rejection.
3. tsc rejected indexing the narrowed JsonWrite union; wire-json.ts narrows via explicit casts (no any).

LIMITATIONS
The write-selection ancestry port is intentionally unimplemented until the UI task supplies it from the
existing Geography picker; selectApplyRequest then returns needsResolution instead of guessing parents.
Only the composite editor document and its type/context observations are exposed; no catalog write,
draft create, activation or per-capability mutation helper. UI wiring/localized presentation is the next
dependency after Architect review.

Worker PASS != Architect ACCEPT.
END_TOOBA_WORKER_RESULT`;

const body = JSON.stringify({
  channelId: "tooba-main",
  taskId: "TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200",
  content,
});

fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body,
})
  .then(async (response) => {
    const text = await response.text();
    console.log(`RESULT_STATUS=${response.status} ${text.slice(0, 400)}`);
    if (!response.ok) process.exitCode = 1;
  })
  .catch((error) => {
    console.log(`RESULT_FAILED=${String(error)}`);
    process.exitCode = 1;
  });
