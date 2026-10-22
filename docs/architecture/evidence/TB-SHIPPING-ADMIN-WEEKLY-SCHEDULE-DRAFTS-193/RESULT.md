# TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193 — worker evidence

Bounded D5 authoring slice: one per-weekday operating schedule persisted ONCE on the owner-scoped
Shipping method draft, shared by every region group of that draft. This is draft authoring storage
plus genuine authorized `GET`/`PUT` — **not** runtime availability, capacity, cutoff, holiday,
customer-promise or delivery-window generation.

| Field | Value |
| --- | --- |
| Task | TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193 |
| Parent | TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1 |
| Channel | tooba-main |
| Bridge row | `7bc484d0-8b19-4211-ac3b-413fc19bc16a` |
| Expected base | `8dec7f8d86646d78588e9186d038352a6a7c6d80` |
| Base SHA | `8dec7f8d86646d78588e9186d038352a6a7c6d80` |
| Claim UTC | `2026-10-22T08:50:55Z` |
| Result UTC | see RESULT payload (`Result-UTC`) |
| Mode | BOUNDED_OWNER_DRAFT_PER_WEEKDAY_OPERATING_SCHEDULE |
| Status | PASS (worker); Architect ACCEPT is **not** claimed |

## 1. Authorized surface

| Verb | Route | Permission |
| --- | --- | --- |
| GET | `/v1/admin/shipping-services/authoring-drafts/{draftId:guid}/weekly-schedule` | `fulfillment.view` |
| GET | `/v1/seller/shipping-services/authoring-drafts/{draftId:guid}/weekly-schedule` | `fulfillment.view` |
| PUT | `/v1/admin/shipping-services/authoring-drafts/{draftId:guid}/weekly-schedule` | `fulfillment.manage` |
| PUT | `/v1/seller/shipping-services/authoring-drafts/{draftId:guid}/weekly-schedule` | `fulfillment.manage` |

One shared Application query (`GetShippingAuthoringWeeklyScheduleQuery`) and one shared command
(`ReplaceShippingAuthoringWeeklyScheduleCommand`) serve both audiences. Owner/mode comes only from
`IShippingAuthoringOwnerAuthorizer`; the permission is checked independently and **before** `ISender`
dispatch. The body carries no owner/tenant/mode/seller/geography/language/currency field. SingleStore
sellers stay `404` before seller resolution; a foreign or invented draft id is the same safe `404`.

## 2. Week / timezone / null semantics matrix

| Case | Result |
| --- | --- |
| Absent schedule | `configured = false`, no timezone, `days = []` (truthful UNCONFIGURED; no fabricated 7-day default) |
| Configured all seven closed | `configured = true` + actual timezone + seven closed rows (distinct from unconfigured) |
| Configured with some closed days | Seven rows; closed days still return their remembered pair when one is stored |
| `timezoneId` blank / unknown / > 64 chars / null | Rejected before mutation: `ScheduleTimeZoneRequired` / `ScheduleTimeZoneInvalid` (shared `ShippingScheduleRules.NormalizeTimeZone`) |
| Missing weekday, duplicate weekday, `weekday < 1` or `weekday > 7`, non-seven collection | `validation.failed` (transport) or `AuthoringScheduleWeekdaySetInvalid` (domain/relational backstop) |
| `isWorking` absent (`null`) | Rejected `AuthoringScheduleDayInvalid` — never deserialized into an implicit closed day |
| `days` null / `days` contains `null` element | Rejected `AuthoringScheduleWeekdaySetInvalid` / `AuthoringScheduleDayInvalid` before any tracked write |
| Working day with missing start or end | Rejected `AuthoringScheduleDayInvalid` |
| Working day with `end == start`, `end < start`, or an overnight shape | Rejected `AuthoringScheduleDayInvalid` (no reversal, no overnight extension) |
| Closed day with both hours absent/null | Retains the previously stored pair if any; `null` on first configuration is allowed |
| Closed day with only one bound supplied | Rejected `AuthoringScheduleDayInvalid` (partial pair) |
| Closed day with a valid explicit pair | Replaces only that day's remembered pair; day stays closed |
| Re-opening a closed day | Requires a valid explicit active pair; no invented default |
| Identical normalized PUT | Truthful no-op: no row update, revision unchanged (`expectedRevision` is still checked) |

ISO identity is `1 = Monday … 7 = Sunday` (Saturday `6`, Friday `5`), stored directly; no
`DayOfWeek` blind cast, no locale/inter­face-culture influence on identity, timezone or stored hours.
Working interval is `[start, end)` stored as `time` (`TimeOnly`, invariant precision).

## 3. Shared-domain timezone/range reuse and legacy parity

`ShippingScheduleRules.NormalizeTimeZone` was extracted from `ShippingOperatingSchedule` (private
validator) into a Domain-level static helper. `ShippingOperatingSchedule` now delegates to it, so the
`TimeZoneInfo.FindSystemTimeZoneById` resolution, 64-character bound and the
`ScheduleTimeZoneRequired` / `ScheduleTimeZoneInvalid` codes are byte-identical in behavior for the
legacy aggregate and the new draft authoring path. The legacy aggregate, its windows, its table and
constraints are untouched; no legacy row is collapsed, backfilled or rekeyed.

Legacy parity proof: `ShippingOperatingScheduleTests` → 74/74 pass (unchanged expectations).

## 4. Persistence and relational parity

New additive migration `20261022085811_AuthoringWeeklySchedule` (+ designer + snapshot) creates only
Shipping-owned tables:

* `shipping.authoring_weekly_schedules` — PK `draft_id`, `time_zone_id` (varchar 64),
  `updated_at_utc`; FK `fk_authoring_weekly_schedules_draft` → `authoring_drafts` (cascade);
  `ck_authoring_weekly_schedules_timezone_length`.
* `shipping.authoring_weekly_schedule_days` — composite PK `(draft_id, weekday)`;
  FK `fk_authoring_weekly_schedule_days_schedule` → `authoring_weekly_schedules` (cascade);
  `ck_authoring_weekly_schedule_days_weekday_range` (`1..7`),
  `ck_authoring_weekly_schedule_days_hours_paired` (`(start_time IS NULL) = (end_time IS NULL)`),
  `ck_authoring_weekly_schedule_days_range` (`start_time IS NULL OR end_time > start_time`),
  `ck_authoring_weekly_schedule_days_working_hours`
  (`is_working = FALSE OR (start_time IS NOT NULL AND end_time IS NOT NULL)`).

No JSON blob, no foreign FK, no duplicated geography, no per-language weekday rows. No existing
migration file was modified; the schema total is now 11 migrations and 8 `FOREIGN KEY` declarations,
all intra-module.

Invariant layering (documented, not faked in SQL): shape/presence/pairing/time-format at transport
validation; timezone resolvability, exactly-seven unique ISO weekdays, interval and remembered-hour
rules at canonical domain preflight; weekday range, paired hours, interval ordering, working-needs-hours
and timezone length as enforced PostgreSQL `CHECK`s; uniqueness/ownership by the composite PK + FK.

## 5. Atomicity and concurrency

`PreflightWeeklySchedule` validates the WHOLE week before the first mutation, then
`ApplyWeeklySchedule` + root `revision` bump + schedule header + seven day rows commit through ONE
implicit `SaveChanges`. No manual transaction, no per-day write, no partial success. A bad seventh day
leaves the same aggregate/tracker, timezone, revision, translations, groups/titles, rate rows and
geography snapshot unchanged so a later `SaveChanges` publishes nothing.

The concurrency token is the SAME root `draft.Revision` used by regions, rates and metadata; a stale
revision returns the canonical `AuthoringDraftRevisionConflict` (409). A relational
`DbUpdateConcurrencyException` clears the tracker and returns the same conflict code.

## 6. Executed verification

> **Correction (TB-193-R1, R193-01).** The concurrency fact originally recorded in this section for
> the schedule-versus-rate race was structurally weak: it released a gate *before* either directory
> call read the root, asserted only "at least one success", tolerated an empty loser set, and accepted
> a revision that both writers could produce. The requirement stands and the production behavior is
> unchanged, but that fact was replaced (see §6.1) with the established real `SavingChanges` pre-write
> barrier and strict winner/loser assertions. The numbers below are the re-executed post-repair run;
> the original 609/609 report remains history, and its concurrency claim is superseded by §6.1.

PostgreSQL 17 disposable task container `tb193-pg` (`postgres:17-alpine`) published on
`127.0.0.1:55432`, database `shipping_tb190r1`, user `tb190r1` (fixture credentials from the prior
task target; no shared `:5432`/`tooba_alpha` access, no credential logging). Module migrations were
applied to that disposable database only.

| Command (focused) | Count |
| --- | --- |
| `dotnet test … --filter FullyQualifiedName~ShippingAuthoringWeeklySchedule` (HTTP + PostgreSQL) | 20/20 pass (17 endpoint + 3 storage, including the repaired R193-01 race fact) |
| `--filter FullyQualifiedName~ShippingOperatingScheduleTests` (legacy parity) | 74/74 pass |
| `--filter FullyQualifiedName~ShippingRequestClassificationTests` | 11/11 pass |
| `--filter FullyQualifiedName~ShippingPersistenceModelTests` | 52/52 pass |
| `--filter FullyQualifiedName~ShippingErrorCatalogAndResourcesTests` | 11/11 pass |
| ONE final whole Shipping suite (`Tooba.Shipping.Tests`) | **609/609 pass**, 0 skipped, 18 s |

New test files: `Tests/Authoring/ShippingAuthoringWeeklyScheduleEndpointTests.cs` (17 cases) and
`Tests/Authoring/ShippingAuthoringWeeklyScheduleStorageTests.cs` (3 executed PostgreSQL cases).

Covered by the 17 endpoint cases: admin/seller GET unconfigured vs configured; PUT then readback;
timezone required/invalid; seven-closed allowed; remembered closed hours surviving reload;
independent Saturday/Sunday hours with Friday closed; missing/duplicate weekday, omitted flag, null
collection/elements, missing/equal/inverted/overnight time rejection before mutation; interface
culture invariance; view-only PUT denied pre-dispatch; foreign draft and SingleStore seller safe 404;
no-op PUT keeping the revision.

Covered by the 3 executed PostgreSQL cases:
1. `Full_week_roundtrips_through_a_new_context_and_survives_unrelated_capability_writes` — seven
   distinct day keys with remembered times read back in a NEW context, and an unrelated capability
   write preserving the schedule and vice versa.
2. `Raw_storage_rejects_weekday_pairs_ranges_flags_duplicates_and_foreign_or_blank_timezone` — raw
   SQL negatives, each attempted individually with its own rollback, asserting the exact SQLSTATE
   (`23514`/`23505`/`23503`) **and** the owned constraint identity: weekday out of range, unpaired
   hours, inverted/equal interval, working without hours, duplicate `(draft, weekday)`, foreign
   draft, blank/over-long timezone.
3. `Concurrent_schedule_and_rate_writes_leave_exactly_the_winner_state_and_one_canonical_conflict`
   (TB-193-R1 repair) — a schedule `PUT` racing ONE rate writer on the same original root revision,
   deterministically synchronized at the **first `SavingChanges`** of each participant (after that
   participant read and staged revision 1, before either wrote anything), with the ORIGINAL concurrency
   value asserted as `1` for both and exactly `2` barrier arrivals.

## 6.1 Strict concurrency proof (R193-01 repair)

The replaced fact asserts, instead of "something won":

* both participants really arrived at their first `SavingChanges` with the original root token `1`
  loaded (`TwoReaderGate.Arrivals == 2`, both interceptors' `OriginalRevision == 1`), so a genuine
  two-reader race is proven rather than two operations merely started together;
* **exactly one** winner (`Assert.Single(!IsFailure)`) and **exactly one** loser
  (`Assert.Single(IsFailure)`), with the loser's error exactly
  `AuthoringDraftRevisionConflict` — a both-succeed outcome fails the test;
* the persisted root revision is exactly `2`, so the shared token advanced exactly once;
* the persisted state is asserted **conditionally on the actual winner, in both directions**:
  schedule-winner ⇒ header + `Asia/Tehran` + seven rows with Saturday `09:00–13:00` working and the
  other six closed, **and** zero rate rows with `CurrencyCode == null`; rate-winner ⇒ one
  `12.50` row with `USD` **and** `WeeklySchedule == null`;
* raw row counts confirm the absence of hybrid state independently of the projection:
  `authoring_weekly_schedule_days` = 7 or 0 and `authoring_rate_rows` = 0 or 1, exactly matching the
  winner;
* the ACTUAL losing `DbContext` (`loser.Index` selects it) has no tracked entry in a non-`Unchanged`
  state, so a later `SaveChanges` on it can publish nothing, and it re-reads the winner's revision `2`
  and winner's capability shape.

Determinism: the focused fact was executed five consecutive times on the same live disposable
database (5/5 pass, ~0.86 s each) and inside the whole suite. No production, schema, API, contract or
migration file changed in this repair — only the test and this evidence correction.

## 7. Classification manifest

`docs/architecture/shipping/shipping-request-classification.json` amended with the two new
requests (both `VALIDATOR_REQUIRED`) and exact actual sets: shipped routes `35 → 39`,
endpoint-reachable requests `26 → 28`, request validators `23 → 25`; declared validators unchanged in
kind. No baseline widening and no module certification promotion.

## 8. Locks respected

No frontend, taxonomy, calculation/payment/carrier/free-mode, quote/evaluator/publish/activation,
allocation/financial/Checkout/payment/deployment or certification change. No Host or foreign-module
Application/Infrastructure/Domain/Endpoints/DbContext/transaction/join. No new project or package.
Runtime-Merchandise-Basis-And-Measurement-Gate stays `OPEN_NOT_IMPLEMENTED`; TB-185 performance work
stays paused; drafts remain inactive configuration. Shared `:3000/:3419/:5088/:5432`, Redis/RabbitMQ,
bin/obj/.next and unrelated dirty/untracked/stashes untouched; no reset/rebase/stash/force-push.

## 9. Explicitly not implemented (pending, not claimed)

Cutoff calculation/field, holidays and exception dates, per-region schedule overrides, multi-window
legacy conversion, preparation time, delivery-duration reinterpretation, customer date/window
generation, capacity/reservation, warehouse hours, order promise changes, DST/holiday/customer-slot
integration, Admin UI/editor activation, production acceptance.

Stop state: `STOP_FOR_ARCHITECT`. No automatic next task.
