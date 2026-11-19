# TB-281-R5 RESULT

Status: **PASS** within issued A/B/C narrow source-confirmed repair.
Decision: `SELLER_ADMIN_UPDATE_ITEM_OCCURRENCE_REPLAY_LENGTH_PREFIXED_UPDATE_HASHES_SCHEDULE_REPLAY_BEFORE_GATES_STORY_OCCURRENCE_UNIQUE_ARBITRATION_MEDIA_AMBIENT_AND_ENLISTMENT_REFUSE_AND_TRUTHFUL_FRESH_CONTEXT_GRID_TWO_CONTEXT_STALE_AND_MEDIA_CALLER_ENTITY_PROOF_LANDED_WITH_FOCUSED_48_48_ZERO_MIGRATION`

NOT Architect ACCEPT. NOT user acceptance. UI delivery PLANNED_NOT_ISSUED.

## Base / HEAD

- Expected-Base-SHA: `aad32713b7486c429dbbfb5e948e95e30eb6bb67` (verified ancestor of claim)
- Claim HEAD at start: `d78ecf39e165c7d10ede61e5de6bacf6856122bb` (docs-only R5 issuance)
- Bridge row: `29d18780-b564-490c-a85a-f18337b22f2b`
- Mode: NARROW_SOURCE_CONFIRMED_REPAIR_WITH_REAL_COUNTEREXAMPLES

## Closed in R5

### A — replay / hash / schedule / unique
- A1: `SellerUpdateAsync`, `AdminUpdateItemAsync`, `SellerUpdateItemAsync` require occurrence, replay before gates, record+save together
- A2: update hashes length-prefix every effective field (title/locale/market/cover-id/cover-url/CTA + targets/version); delimiter-collision pair conflicts
- A3: `AdminSetScheduleAsync` replays exact occurrence before version/content gates; original body/key replays after success; changed time conflicts; stale NEW key refused
- A4: `SaveChangesWithOccurrenceArbitrationAsync` folds exact `story`/`command_occurrences`/`pk_command_occurrences` 23505; unrelated unique propagates; fault-injection winner fold one row

### B — Media transaction authority
- Refuse EF `CurrentTransaction`, ambient `Transaction.Current`, and provider `EnlistedTransaction` before object store
- Typed code remains `MediaErrorCodes.ValidationFailed` (no fabricated ambient code)

### C — focused counterexamples
- C1 InMemory: `StoryMarketplace281R5ReplayTests` (6)
- C2 PostgreSQL rollback-only: fresh-context public+expired+`AdminStoryGridQueryEngine` shop-name+seller query; two-context stale owning command; Media EF/ambient/enlistment (`StoryMarketplace281R5RelationalTests` 3)
- C3 Media: real caller-owned tracked entity across pre-save failure; fresh-context after-save durability; independent occurrence committedRows=0 (`MediaStorySellerUpload281R5Tests` 3)
- Negative control: blanked `RequireOccurrenceKey` → A1 FAIL (Assert.Throws type mismatch); restored → PASS

## Proof summary

- Focused R5 + Story AMSC: **48/48**
- Schema/migration this task: ZERO
- Live Story/Media business write: ZERO
- Host refreshed with `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1` (see RECEIPTS)
- R4 historical evidence unchanged; see `CORRECTION-ADDENDUM.md`

automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT

## Final SHA

- Implementation code SHA: `fa32794e059f8fdf52858cabfcc83494be05ed03`
- Final origin/main (docs tip): `d14344a951be380bd67854abef67d331b75db885`
