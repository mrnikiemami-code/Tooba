# Result — TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192

Date: 2026-10-22 (UTC)
Verdict: PASS (bounded persisted language-keyed owner presentation only)
Base: cb3b52908d7cc88aba26d8d37052289b6abfcd81

## Scope delivered

Owner-local multilingual presentation for ONE existing Shipping draft, keyed by real LanguageId:

* method name + optional description per language (`authoring_draft_translations`),
* optional per-group title per language (`authoring_group_titles`).

No Fulfillment catalog row is mutated or cloned. No cross-schema FK, no copied registry, no
translation fabrication, no frontend, no runtime quote/activation, no tax/taxonomy/schedule.

## API

One Application query and one PATCH command serve both audiences:

| Audience | Route | Permission |
| --- | --- | --- |
| Admin | `GET/PATCH /v1/admin/shipping-services/authoring-drafts/{draftId:guid}/presentation` | view / manage |
| Seller | `GET/PATCH /v1/seller/shipping-services/authoring-drafts/{draftId:guid}/presentation` | view / manage |

* Body: `expectedRevision`, `methodTranslations[]`, `groupTitles[]`. **No** owner/mode/tenant/currency.
* Each entry changes only its own `LanguageId`; a missing entry means NO CHANGE. `[]` is a valid no-op.
* Missing/null lists (and null elements) are rejected by the real MediatR validation pipeline.
* Description/title `null`/empty clears only that selected-language value.
* GET returns raw stored text + the ACTUAL active registry + default id + `HasDefaultLanguageName`
  (a truthful comparison, not a fallback display name).

## Language authority and retention

* Active/default language facts come from the unchanged TB-188 `IShippingAuthoringContextSource`
  (`ILanguageLookup` via Contracts) — one projection extracted into
  `ShippingAuthoringContextProjection` and reused by the context query, so the readiness algorithm is
  not duplicated. Behavior/status codes of TB-188 are unchanged and its tests still pass.
* Unknown/inactive language ids are rejected **before** any tracked mutation
  (`shipping.authoring.presentation_language_invalid`).
* Text stored for a now-inactive language is retained; a later reactivation exposes it unchanged.
  Reads truthfully expose which ids are currently configured.
* Interface locale is never consulted for content selection; switching fa/en culture leaves stored
  text, registry identities and rules identical.

## Persistence

* ONE additive migration `20261022081351_AuthoringPresentationTranslations` (+ designer + snapshot).
  Historical migrations are byte-identical; `migration-forward.sql` is the emitted forward script.
* Composite PK `(draft_id, language_id)` / `(group_id, language_id)` enforce one text per item+language.
* Own cascade FKs only: draft→translations and group→titles. Group removal deletes only that group's
  titles; method text and other groups survive retained-group reorder/rate edits.
* CHECK constraints reject blank-only and over-long text without nullable-UNKNOWN loopholes
  (`char_length(btrim(x)) > 0` is FALSE, never UNKNOWN, because the column is NOT NULL).
* Same root `revision` concurrency token as regions/rates; one implicit `SaveChanges`, no new
  `BeginTransaction`, no dictionary replacement.
* No-op documents do not bump the revision (truthful no-op). A real change bumps it exactly once.

## Executed proof

`docs/architecture/evidence/TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192/migration-forward.sql`

Isolated disposable PostgreSQL 17 container on loopback `127.0.0.1:55432` (fresh, task-owned,
removed afterwards; no shared :5432 and no shared data touched).

```
dotnet build Shipping.Tests  -> 0 errors
dotnet test  focused (2 new) -> 2/2 passed
dotnet test  whole Shipping  -> 579/579 passed, 0 failed, 0 skipped (12 s)
```

New executed relational proofs:

1. `Presentation_patch_touches_only_the_selected_language_and_keeps_regions_rates_and_currency`
   — exact stored language keys/text, English preserved while fa written, group title applied, regions
   and rate rows and bound currency unchanged, duplicate/inactive last entry rejected with an empty
   change tracker and an unchanged revision, and group removal cascading only its own titles.
2. `A_same_revision_presentation_patch_racing_a_rate_write_leaves_exactly_the_winner_state`
   — deterministic two-reader barrier on the SAME root revision; one winner, canonical 409 conflict for
   the loser, exact winner-dependent state with no hybrid text/row and a safe loser tracker.

## Honest limitations

The language ids used by the tests are controlled fixtures; they prove handler/domain/persistence
behavior and lawful source wiring, not a live merchant Localization database or browser session.
Configured-registry runtime/UI acceptance remains a later integration gate. This is not Admin,
production or module certification. Runtime-Merchandise-Basis-And-Measurement-Gate: OPEN_NOT_IMPLEMENTED.

## Correction appended by TB-192-R1 (2026-10-22)

Scope correction to the two facts above: both call `ShippingAuthoringDraftDirectory` directly with a
manually supplied active-language `HashSet`. They therefore prove domain/permission-free selected-language
persistence and the same-revision race, and they do NOT prove the presentation HTTP endpoints, the real
owner authorizer, the MediatR validation pipeline, or registry-driven transitions. Their PASS and the
historic 579/579 remain valid reported history, not API/registry proof.

The missing executed proof was added in
`docs/architecture/evidence/TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1/RESULT.md`:
real admin/seller presentation GET/PATCH, one/two/three-plus dynamic language matrix, deactivate/reactivate/
default-change retention, selected clear and true no-ops, interface-culture invariance, pre-dispatch
denials and malformed-input preflight, plus an independent raw PostgreSQL storage matrix on
`shipping.authoring_draft_translations` and `shipping.authoring_group_titles` and retained-text
reorder/rate-edit proof. Production executable behavior is unchanged; only the `PreflightPresentation`
XML remark was corrected to the approved active-only PATCH plus passive retention semantics.
