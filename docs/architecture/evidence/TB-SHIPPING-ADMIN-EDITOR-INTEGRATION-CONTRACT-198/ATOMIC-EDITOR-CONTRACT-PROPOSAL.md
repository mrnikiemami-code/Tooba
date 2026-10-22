# TB-198 — One proposed atomic editor document contract (proposal, documentation only)

Status: **PROPOSAL — NOT APPROVED, NOT IMPLEMENTED.** Illustrative JSON is documentation, not a wire contract.
Base `e33ef067`. Nothing here authorises routes, runtime changes, activation, deployment or a decision
amendment.

## 1. Chosen shape

One Shipping-owned draft document with **one root expected revision**, read in full and written in full:

- `GET  /v1/{admin|seller}/shipping-services/authoring-drafts/{draftId}/editor-document` — full read (read-only).
- `PUT  /v1/{admin|seller}/shipping-services/authoring-drafts/{draftId}/editor-document` — full replace (Apply).
- Existing routes stay as they are (independent capability APIs, legacy behaviour, migrations, default-OFF
  gates untouched). They become the *only* path for narrow callers; the editor uses the document route.

Why one document and not "partial composition of N existing handlers": each existing handler commits and
advances `Revision` independently. Looping them (or `Promise.all`) yields partial success and revision churn,
exactly what decision section 9 forbids. Composition must happen **inside one Shipping handler over one own
aggregate/save**, reusing the existing `Preflight*`/`Apply*` split — not a second rule engine.

### Reads — do existing routes suffice?

Yes, but only as *six separate documents* (draft tree, per-group rows, presentation, schedule, settings, type)
that can disagree on `Revision` mid-load. Recommendation: keep them, and add ONE additive read document so the
editor takes a single consistent snapshot. The editor read is a projection of existing queries; it introduces
no new authority.

## 2. Illustrative shapes (documentation only)

`GET .../editor-document` → `200`:

```json
{
  "draftId": "3f1c...",
  "owner": { "kind": "marketplace" },
  "revision": 7,
  "binding": { "serviceCode": "post", "optionCode": null, "immutable": true },
  "type": {
    "shippingTypeId": "9a2e...",
    "resolution": "Active",
    "type": { "shippingTypeId": "9a2e...", "code": "post", "sortOrder": 0, "revision": 2, "translations": [] }
  },
  "methodSettings": { "isConfigured": true, "freightPaymentMode": "CollectOnDelivery", "calculationMode": "Manual" },
  "presentation": {
    "methodTranslations": [{ "languageId": "1b2c...", "name": "پست پیشتاز", "description": null }],
    "groupTitles": [{ "groupId": "aa11...", "languageId": "1b2c...", "title": "تهران" }],
    "contentLanguages": [{ "languageId": "1b2c...", "isDefault": true }],
    "defaultContentLanguageId": "1b2c..."
  },
  "weeklySchedule": {
    "isConfigured": true,
    "timeZoneId": "Asia/Tehran",
    "days": [{ "weekday": 6, "isWorking": true, "startTime": "09:00", "endTime": "18:00" }]
  },
  "regionGroups": [
    {
      "groupId": "aa11...",
      "order": 0,
      "areas": [{ "kind": "City", "id": "..." }],
      "currencyCode": "IRR",
      "rows": [
        {
          "rateRowId": "cc22...",
          "minWeightGrams": 0, "maxWeightGrams": 5000,
          "minItemCount": null, "maxItemCount": null,
          "minMerchandiseAmount": null, "maxMerchandiseAmount": null,
          "freightAmount": 120000, "packagingAmount": 0,
          "minDeliveryDays": 2, "maxDeliveryDays": 4
        }
      ]
    }
  ]
}
```

`PUT .../editor-document` (Apply) — illustrative request:

```json
{
  "expectedRevision": 7,
  "type": { "shippingTypeId": "9a2e..." },
  "methodSettings": { "freightPaymentMode": "CollectOnDelivery", "calculationMode": "Manual" },
  "presentation": {
    "methodTranslations": [
      { "languageId": "1b2c...", "name": "پست پیشتاز", "description": null }
    ],
    "groupTitles": [{ "groupId": "aa11...", "languageId": "1b2c...", "title": "تهران" }]
  },
  "weeklySchedule": {
    "timeZoneId": "Asia/Tehran",
    "days": [
      { "weekday": 1, "isWorking": false },
      { "weekday": 6, "isWorking": true, "startTime": "09:00", "endTime": "18:00" }
    ]
  },
  "regionGroups": [
    {
      "groupId": "aa11...",
      "areas": [{ "kind": "City", "id": "..." }],
      "rows": [
        { "rateRowId": "cc22...", "minWeightGrams": 0, "maxWeightGrams": 5000,
          "freightAmount": 120000, "packagingAmount": 0, "minDeliveryDays": 2, "maxDeliveryDays": 4 }
      ]
    }
  ]
}
```

Rules that make one Apply truthful:

- **Owner**: never in either body; resolved by `RequireAdminOwnerAsync`/`RequireSellerOwnerAsync` before body
  read and dispatch (`fulfillment.view` for GET, `fulfillment.manage` for PUT, checked independently).
- **Binding**: immutable, not writable after create. `POST .../authoring-drafts` stays the only create path
  (`ServiceCode`+`OptionCode?`, owner+binding uniqueness preserved). Foreign catalog creation is outside this
  transaction and is not attempted.
- **Revision**: exactly ONE `expectedRevision` on the root; stale ⇒ `409` **before any optional foreign I/O**.
  Apply advances `Revision`/`UpdatedAt` exactly once; an identical document is a truthful no-op (no revision,
  no timestamp change). Never reset the revision afterwards; no client compensation.
- **Nested identity**: `groupId`/`rateRowId` present ⇒ must belong to this draft/group (unknown/foreign/
  duplicate ⇒ `400` before mutation); absent ⇒ new server id, never a fake client GUID. No geography on rows.
- **Presence semantics** per subdocument, mirroring today's contracts: field **absent** = unchanged (for a
  full replace, absent top-level subdocument = unchanged), explicit `null` = clear where the field is
  clearable (type, description/title, method settings), `[]` = empty set that is *not* a fallback (empty rows =
  incomplete, not free; empty days = invalid, not closed), `isWorking` `null`/absent = invalid, not "closed".
- **Multilingual**: full save carries the **complete** desired text set per language it touches; stored text for
  languages omitted from the document is preserved (patch semantics inside a full save, so an inactive
  language's saved text is never erased). Only raw `LanguageId`; active/default language comes from TB-188
  context + registry. No fa/en hardcoding, no locale→language or locale→currency inference.
- **Currency**: server-trusted per group (existing behaviour). Never in the body.
- **Type**: reuse 197-R1 — resolve exactly one ACTIVE definition **before** the write, retain that immutable
  fact, compose the response from it plus the saved root; no post-commit catalog read; later GET may truthfully
  show `Inactive`/`Missing`. Choice is not readiness.
- **Geography**: reuse the existing Contracts ancestry proof for every submitted area **before** any tracked
  mutation; no cross-schema joins or cross-owner inheritance.

## 3. Execution order inside the handler (all read-only facts first)

1. owner scope from the authorizer (transport-level), then `GetAsync` inside the owner filter;
2. `Revision` check (stale ⇒ `409`, before foreign I/O);
3. resolve **all** dependency facts: Geography ancestry for every area, active content languages/default,
   server-trusted currency, and the single ACTIVE shipping type (in that order, each failing closed with its
   existing code);
4. **preflight the whole proposed graph in memory** with the existing `Preflight*` methods (regions, every
   group's rows, presentation, weekly schedule, method settings, type). Any failure ⇒ `Result` failure with
   the existing code and **no** tracked mutation;
5. apply the plan(s) onto the one aggregate → exactly one `Revision`/`UpdatedAt` advance;
6. ONE `SaveChanges` (implicit, own context) — one transaction as actually necessary; no remote writes, no
   distributed transaction, no cross-module transaction;
7. response composed **without any fallible dependency** after commit (197-R1 rule).

Failure paths: provider exception/cancellation propagates through the existing canonical boundary; the
district tracker is cleared so a retry in the same context publishes nothing from the failed attempt;
optimistic concurrency/unique conflict ⇒ existing canonical `409` with a clean loser tracker.

Minimal composition/refactor points (no new engine):

- add ONE Application command `ReplaceShippingAuthoringEditorDocumentCommand` + handler and ONE query;
- add ONE directory method `ReplaceEditorDocumentAsync(...)` implemented in
  `ShippingAuthoringDraftDirectory` that loads the aggregate once, calls the existing `Preflight*`/`Apply*`
  methods in order and saves once — the independent capability methods remain for their own routes;
- reuse `ShippingTypeAssignmentComposition`, `ShippingAuthoringContextSource` (languages/currency),
  `IShippingAuthoringDestinationResolver` (Geography), and the existing typed error codes;
- existing capability routes and their contracts are **unchanged** (no breaking change to
  `ShippingAuthoringDraftItem`, per-group rows, presentation PATCH, schedule, settings, type).

## 4. Future acceptance matrix (NOT executed here)

| # | Future acceptance condition |
| --- | --- |
| 1 | Late-invalid child (last group's row / 7th day) fails the whole Apply: same aggregate object, no later `SaveChanges` publishes 0. |
| 2 | New-context complete roundtrip equals the submitted document (groups/rows/text/schedule/settings/type). |
| 3 | Full-document no-op with an advanced/real clock preserves revision and timestamp exactly. |
| 4 | One-winner shared-root race: editor Apply vs any capability PUT ⇒ exactly one winner, loser `409`, no hybrid tree. |
| 5 | Loser's `ChangeTracker` has no pending entries; later save publishes 0. |
| 6 | Real provider-aborted write ⇒ nothing published, retry with the original revision succeeds once. |
| 7 | Caller cancellation mid-write ⇒ no partial graph, retry lawful. |
| 8 | Owner/permission denial happens before dispatch/body (no foreign read, no tracked entity). |
| 9 | One/two/three-plus configured languages: untouched and inactive-language text preserved. |
| 10 | Type `Active`/`Inactive`/`Missing` after save comes from the pre-validated fact; no post-commit read. |

## 5. Gaps and decisions still requiring approval (no invented rule)

1. **Cutoff**: source confirms only the approved sentence "order cutoff **before the start** of that working
   day's interval" (preview label `scheduleCutoff`, hint/summary). The sample `4` and the UI `min=0 max=48`
   are **fixture/UI facts**, not policy. Unanswered: is it per method or per weekday; integer hours or minutes;
   inclusive bounds; semantics on a closed day; interaction with timezone/DST; whether it is draft-only or a
   runtime promise input. Recommendation: draft-only optional integer "hours before day start", validated
   0..(existing UI bound *if* approved), no execution algorithm until a product-owner decision.
2. **Draft status / activation**: preview `PreviewMethodStatus` ("active"/"inactive") has no approved draft
   authority. Recommendation: expose only a read-only completeness projection; no activation, no "ready"
   claim, no storefront effect.
3. **Owner selection**: admin resolves Store or Marketplace from platform auth; seller resolves its
   authenticated owner. The preview owner dropdown is **not** authorisation to pick an arbitrary
   seller/tenant, and no admin-facing seller-picker source exists today. Recommendation: display the resolved
   scope read-only in the first slice; a seller selector is a separate decision with its own authorisation
   source.
4. **Carrier / free funding / live integration status**: preview-only (explicitly "sample"). No registry,
   credentials or quote seam exists; out of scope.
5. **Configured vs operational readiness**: `Manual`/`CarrierApi`/`Free` and type selection are draft
   selections; none implies quote readiness, funding, activation or carrier integration.
6. **Where the editor lives** (extending the preview screen vs the real flat screen) and the **seller UI entry
   point** are product/UX decisions; the contract above is independent of that choice.

## 6. Recommended first implementation slice (single next slice only)

**Slice A — atomic backend editor document (Shipping module, no UI).**
Paths: `Contracts/Authoring` (one additive read/write contract file), `Application/Authoring/Queries` +
`Commands` + `Validation`, `Application/Authoring/Ports/IShippingAuthoringDraftDirectory` (one method),
`Infrastructure/Authoring/ShippingAuthoringDraftDirectory` (one composed implementation),
`Endpoints/Admin` + `Endpoints/Seller` (two routes each), plus the existing request-classification JSON.
Dependencies: none beyond what exists (197-R1 composition, TB-188 context, Geography resolver).
Timebox: one bounded 20–30 minute slice; acceptance = `Revision` advances once per Apply, stale/stale-race/
late-invalid/preflight-failure behaviour proven with focused same-context and one real PostgreSQL abort fact.
No activation, no storefront, no quote.

Then, only as dependencies: **Slice B** feature adapter/UI wiring onto the document route (frontend skill,
visual review), **Slice C** live configured-registry/owner acceptance. No further task envelopes are emitted
here; delivery is explicitly **not** claimed complete.
