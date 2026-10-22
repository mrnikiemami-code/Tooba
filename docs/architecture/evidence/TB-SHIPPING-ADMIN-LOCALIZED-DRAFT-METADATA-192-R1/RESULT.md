# TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1 — Result

Mode: BOUNDED_PRESENTATION_ENDPOINT_AND_DYNAMIC_LANGUAGE_PROOF_COMPLETION
Base: 87ae74549d34e941a9c7d76ff61eb1f28ce51568 (HEAD == origin/main at claim)
Disposition: PASS (executed proof), STOP_FOR_ARCHITECT — no Architect ACCEPT claimed.

## Executed scope (only these are new facts)

Build: `dotnet build src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj` — 0 errors.

| Fact | Executed? | What it proves |
| --- | --- | --- |
| `One_active_non_fa_language_roundtrips_through_the_real_presentation_routes` | YES | one active NON-fa default; PATCH then GET through real admin route/authorizer/validator/ISender/handler/projection/directory/response factory; trimmed Unicode raw text; `contentLanguages`, `defaultContentLanguageId`, `hasDefaultLanguageName`; region tree + revision otherwise untouched; real adapter call counter advanced |
| `Two_mixed_rtl_ltr_languages_are_written_independently_and_returned_raw_with_registry_order` | YES | two active languages (rtl+ltr); second PATCH leaves the first language byte-identical; group titles per (group, language); owner order; region selection unchanged |
| `Three_plus_languages_with_non_first_default_and_an_inactive_stored_language_retain_text` | YES | 4 known / 3 active; default NOT first; deactivate → raw text retained, excluded from active registry; inactive PATCH → `shipping.authoring.presentation_language_invalid`, revision unchanged; reactivate → EXACT previous text, no auto-translation; default change to a language with no stored name → `hasDefaultLanguageName=false`; reorder follows owner `SortOrder`; writing the new default flips completeness |
| `Selected_clear_and_true_noops_keep_revision_and_timestamp_and_stale_revision_conflicts` | YES | explicit `null` description + `null` title clear ONLY that language/group; other entry and group byte-identical; `[]`/`[]` and an identical active entry are true no-ops (revision AND `UpdatedAt` unchanged); stale `expectedRevision` → 409 `shipping.authoring.draft_revision_conflict` with no change |
| `Interface_culture_and_accept_language_never_select_content_language` | YES | current culture `fa-IR` vs `en-US` (plus `Accept-Language`) under a restored try/finally: GET payload byte-identical, PATCH still writes exactly the selected `LanguageId` |
| `Admin_view_only_and_foreign_owner_or_group_requests_are_denied_before_any_mutation` | YES | view-only PATCH → 403 `admin.authorization.denied` with **0 Send** and **0 handler** (measured separately); another store GET/PATCH → 404 `authoring.draft_not_found`; invented group id → 404 `authoring.region_group_not_found` with no partial edit |
| `Marketplace_seller_can_read_and_write_and_single_store_seller_is_unavailable_before_resolution` | YES | Marketplace seller PATCH+GET succeed through the real seller route; scoped grant → 403 `authoring.owner_scope_denied` before dispatch; SingleStore GET/PATCH → 404 `authoring.owner_scope_unavailable` with `SellerCalls == 0` and `SentCount == 0` |
| `Malformed_presentation_payloads_fail_before_the_handler_and_change_nothing` | YES | 13 shaped failures through the real body reader + FluentValidation (omitted/null collections, null elements, duplicate language, duplicate (group,language), empty language id, blank name, over-length name/description/title): `validation.failed` + exact code, **Sent > 0 but handler == 0**, registry reads == 0, revision unchanged. Inactive-but-well-shaped → handler-level language error. Valid FIRST entry + bad LAST group → 404 with empty ChangeTracker on the SAME context, a later `SaveChanges` publishing nothing, and a following valid PATCH succeeding |
| `Raw_storage_rejects_duplicate_foreign_null_blank_and_over_length_presentation_rows` | YES (PostgreSQL 17) | own-transaction rolled-back raw SQL on the two NEW tables: duplicate `(draft,language)`/`(group,language)` → 23505 with `pk_authoring_draft_translations` / `pk_authoring_group_titles`; own-parent FK → 23503 with the exact FK; `NULL` name/title → 23502 with column identity; blank `'   '` → 23514 with the exact `ck_*_length`; over-length → 22001 (varchar bound fires before CHECK, same 200/2000/200 limit); Unicode method text + group title persist on a NEW-context readback; at-limit 200/2000/200 accepted |
| `Retained_group_reorder_and_a_rate_edit_keep_the_exact_method_and_group_text` | YES (PostgreSQL 17) | after presentation text + titles are saved: retained-group REORDER keeps ids so both languages' method text and both titles survive; a later rate edit on the same group (3900 / IRR) preserves the text; reorder order and new freight are re-read from a new context |

## Composition actually used

* Routes from `MapShippingEndpoints()` (`ShippingEndpointModule`), real admin **and** seller delegates.
* Real `ShippingAuthoringOwnerAuthorizer` (admin seam + seller panel seam + effective-access seam), real
  `ISender`, real `ValidationBehavior`, real presentation query/command handlers, real
  `ShippingAuthoringContextProjection`, real `ShippingAuthoringDraftDirectory`, real `ApiResponseFactory`
  and `ShippingErrorCatalogContributor`.
* Registry facts travel the production `ShippingAuthoringContextSource` (real adapter) with a controlled
  stub `ILanguageLookup` + `ICurrentStoreCommerceContext`. **Fixtures are labelled fixtures: no live
  merchant Localization database and no browser session is claimed.**
* Dispatch is instrumented twice: `SentCount` (behavior registered BEFORE `ValidationBehavior`) and
  `HandlerReached` (behavior registered AFTER it), so "denied before dispatch" and "dispatched but never
  executed" are measured separately.
* Storage facts use the parsed task-owned target `127.0.0.1:55432 / shipping_tb190r1 / tb190r1` on a
  fresh `postgres:17-alpine` container, with a live `current_database()|current_user` check before writes.

## Production change (comment only)

`ShippingAuthoringDraft.PreflightPresentation` XML remarks corrected: the previous text claimed an
*unchanged* inactive-language entry may be accepted, while the executable code rejects **every** included
inactive `LanguageId`. The remark now states active-only PATCH plus passive retention. No executable
behavior, schema, contract, route, permission or DTO changed.

## Reused (NOT re-executed here, unchanged evidence)

* TB-192 `Presentation_patch_touches_only...` and `A_same_revision_presentation_patch_racing_a_rate_write...`
  (directory-level, manually supplied active sets) — still valid for their narrower scope; they are NOT
  the API/registry proof above.
* TB-188 context-projection/one/two/three-plus context tests — the projection evidence, not presentation.
* Historic 579/579 remains reported history; this session executed **589/589** including the 10 new facts.

## Commands / counts / cleanup

* Targeted: presentation endpoints 8/8; storage 2/2.
* Final whole module suite once, DB live: **589 passed / 0 failed / 0 skipped (14 s)**.
* Container `tooba-shipping-tb192r1-pg` removed; port **55432** verified free afterwards.
* No shared `:5432`/`:3000`/`:3419`/`:5088` server touched; no Host start, no migrator against a shared
  database, no reset/rebase/stash/force-push, no Tooba.Host.Tests or other-module suite.

## Open gates (unchanged)

Runtime merchandise basis/measurement: OPEN_NOT_IMPLEMENTED. Live merchant Localization registry/browser
integration: OPEN. TB-185 paused. No TB-193 issued.
