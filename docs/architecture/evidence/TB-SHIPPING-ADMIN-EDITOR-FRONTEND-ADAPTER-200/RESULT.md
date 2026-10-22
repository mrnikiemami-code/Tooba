# TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200 — Worker Result

Mode: BOUNDED_FRONTEND_TYPED_EDITOR_ADAPTER_AND_SESSION_STATE
Base SHA: `ccca74258fb70c3eb64be5c743b9ee2d93235e5a`
Claim: 2026-10-22T23:13:11Z (2026-10-23 02:43 +03:30) · Result: 2026-10-22T23:42:00Z (2026-10-23 03:12 +03:30)
Elapsed: ~29 min (hard timebox 30, new implementation stopped before minute 23)

## Ownership and public interface

One new owner: `src/frontend/features/admin-shipping-authoring/` following `features/admin-sellers`
and `features/admin-languages` placement. Public boundary is `index.ts` only (types + the three API
entry points + reducer/selectors). No `app/**` route, screen, live flat shipping page, preview,
design-system or shared-lib file was touched; no second auth client, generic transport/store/form
framework or duplicate legacy rate-policy API was introduced.

| File | Responsibility |
| --- | --- |
| `api/wire-json.ts` | Strict JSON read/write primitive that keeps exact number text and rejects duplicate keys |
| `api/editor-document-decode.ts` | Fail-closed decode of the real composite document |
| `model/editor-document.ts` | Typed read model (no locale/owner/currency authority fields) |
| `model/editor-edits.ts` | Local edit intent, resolved-ancestry port, client-local keys |
| `api/editor-document-request.ts` | ONE exact PUT body builder (decimal/time text preserved) |
| `api/editor-document-api.ts` | GET/PUT through existing `/v1` upstream + shared admin headers, typed failures |
| `session/editor-session.ts` | Reducer/selectors: baseline vs edits, generation/request scoping, decisions |
| `index.ts` | Public exports only |

## Exact endpoints and wire mapping

- `GET /v1/admin/shipping-services/authoring-drafts/{draftId:guid}/editor-document`
- `PUT /v1/admin/shipping-services/authoring-drafts/{draftId:guid}/editor-document`

Path is built by `editorDocumentPath()` and the request goes through the existing same-origin `/v1`
rewrite (`next.config.ts` rewrites) with `adminRequestHeaders()` from `lib/admin/admin-result.ts`
plus the established dev-actor header. No new endpoint prefix, proxy, owner/tenant/seller/edition/
currency field or header exists in the adapter (`grep` assertion in tests).

Authorities traced and matched (not the TB-198 proposal/preview types):

- `Contracts/Authoring/ShippingAuthoringEditorDocumentContracts.cs`
- `Application/Authoring/Models/ShippingAuthoringEditorDocumentModels.cs`
- `Application/Authoring/Queries/GetShippingAuthoringEditorDocumentQuery.cs` (composition order)
- `ShippingAuthoringDraftView.cs`, `ShippingAuthoringTypeQueries.cs`, `ShippingAuthoringContextView.cs`
- Host JSON policy: camelCase, `JsonStringEnumConverter`, `DefaultIgnoreCondition.WhenWritingNull`

Mapping consequences honoured:

- Enums are matched as their real wire strings (`Country|AdministrativeArea|City|ServiceArea`,
  `Prepaid|CollectOnDelivery`, `Manual|CarrierApi|Free`, `Store|Marketplace|Seller`,
  `Unconfigured|Active|Inactive|Missing`). Unknown variants fail the decode.
- `decimal` amounts keep their exact token text (`0.50` stays `0.50`, `1850000.00` stays
  `1850000.00`) and are re-emitted as unquoted number tokens. `TimeOnly` keeps its exact wire text.
- Server-null sections are absent (WhenWritingNull); absent optional section = `null`, required
  collection missing = untrusted. Literal `null` is accepted only where the contract allows it.
- Raw stored text is never trimmed/coerced, and translations for language ids that are no longer in
  the active registry are retained exactly as stored.
- Draft id in the payload must equal the requested draft id; a mismatch is untrusted.
- Type assembly is validated against its own `revision`/resolution invariants; no Manual/Prepaid/
  Active is fabricated when the server says otherwise.

## Session state

| Concern | Behaviour |
| --- | --- |
| Load | `idle → loading → loaded|denied|notfound|error`; failure keeps `baseline = null` |
| Baseline vs edits | Confirmed server document and local edit intent are separate; `unsaved` derived from intent |
| Save | `clean → saving → clean|validationFailed|conflict|outcomeUnknown|error`; failures always preserve edits |
| Success | Adopts the validated response (server ids, revision); a true no-op with the same revision is accepted |
| Scoping | Results carry draft id + generation + save request id; stale/other-session results return the same state |
| Duplicate Apply | Second `saveStarted` while `saving` returns the identical state (not queued) |
| Discard / switch | Dirty close or switch sets `discardUnsaved` decision; only `discardConfirmed` drops work; cancel keeps it |
| Locale | Interface locale is not in the model or body; it cannot change ids, money, weekdays, times or content-language edits |

## Geography boundary (kept explicit)

GET areas are `(kind, id)` only. `buildEditorDocumentBody()` requires an `AreaAncestryResolver`
supplied later by the authoritative Geography picker. An unresolved selection produces
`{status:"needsResolution", pending:[{groupId, area}]}` and NO body — so a read selection can never be
echoed into a destructive PUT or silently dropped. Group/row client keys are never serialized as ids.

## Tests executed

`node --experimental-strip-types --test features/admin-shipping-authoring/api/editor-document-adapter.test.ts features/admin-shipping-authoring/session/editor-session.test.ts`
→ **13 passed / 0 failed**.

Covered: real composite wire decode (3 active languages, non-first default, mixed rtl/ltr, inactive
stored text retained, decimal scale and fractional time preserved); fail-closed on unknown enum,
missing required row field/collection, unresolved default language, draft-id mismatch, duplicate key;
exact PUT body text incl. `expectedRevision`, `0.50`/`1850000.00` tokens, explicit
`{"typeAssignment":{"shippingTypeId":null}}`, PRESERVE omission of absent optional sections, `[]` as
no text change, no owner/tenant/seller/currency/edition/keys; exactly ONE PUT with the real URL,
method, headers and body; `needsResolution` blocks construction; 401/403/404/409(+property errors)/
400/422/503/500 mapping both verbs; PUT network → `outcomeUnknown` with one attempt and no replay;
cancelled GET → `network`, not fabricated `notFound`; malformed 2xx → `untrusted` (GET) /
`outcomeUnknown` (PUT); conflict/validation preserve local edits; success adopts real ids/revision;
stale A results cannot overwrite B; duplicate Apply gated; dirty switch/close decisions; untouched
language text never echoed as a patch.

`npx tsc --noEmit` → exit 0.

Architecture guards (`frontend-source-size`, `frontend-import-boundary`, `frontend-flat-folder`,
`frontend-feature-boundary`, `frontend-test-discovery`): 7 passed / 1 failed. The single failure is a
pre-existing, unrelated baseline growth in files this task never touched
(`app/admin/category-admin-screen.tsx` 2230>2215, `app/admin/landing-pages/admin-landing-page-composer.tsx`
1442>1440, `app/storefront/storefront-landing-blocks.tsx` 1033>1026,
`lib/storefront-composition/shared-composition-renderer.tsx` 918>907). No new file of this feature is
oversized and no baseline/config was changed. Not fixed: out of scope.

## Not run (deliberately)

No frontend build, whole discovery suite, Next server, browser/HTTP probe, backend build/test/DB/
container/migration, and no live network probing. No rendered page exists in this slice, so no visual
acceptance and no critical-storefront suite is claimed or required.

## API / visual impact

Additive feature module only. No visual change, no live screen change, no route, no schema/data/flag
change, no permission or server-authority change. Production-Functional-Acceptance: PENDING.

## Corrections made during execution

1. Session/adapter fixtures initially encoded `decimal` values as JSON strings; corrected so the
   fixtures write real number tokens like the server does.
2. Duplicate-key fixture is rejected by the wire reader itself; the negative test now treats a null
   parse as rejection.
3. `tsc` rejected indexing the narrowed `JsonWrite` union; `wire-json.ts` now narrows through an
   explicit readonly-array/record cast (no `any`).

## Limitations

- The write-selection ancestry port has no implementation yet by design; the UI task must supply it
  from the existing authoritative Geography resolution path. Until then `selectApplyRequest` returns
  `needsResolution` rather than guessing parents.
- Only the composite editor document plus its type/context observations are exposed; no catalog
  write, draft create, activation or per-capability mutation helper exists.
- UI wiring/localized error presentation is the next dependency after Architect review.
