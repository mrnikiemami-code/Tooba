# TB-281-R4 RESULT

Status: **PASS** within issued A/B/C three-gap closure.
Decision: `END_TO_END_VERSION_OCCURRENCE_TRANSPORT_PLATFORM_CTA_ADMIN_SCHEDULE_CONTENT_GATE_MEDIA_SAVECHANGES_FAILURE_INJECTION_EXACT_PG_UNIQUE_AND_THREE_ROLLBACK_ONLY_PACKAGES_LANDED_WITH_FOCUSED_PROOF_ZERO_MIGRATION`

NOT Architect ACCEPT. NOT user acceptance. UI delivery PLANNED_NOT_ISSUED.

## Base / HEAD

- Expected-Base-SHA: `14b2395eea3585b7e12af71d135c84c77ce7f9c2` (verified ancestor of claim)
- Claim HEAD at start: `dbb612d6de21f9d223aa392b002339624e843392` (docs-only R4 issuance)
- Bridge row: `7d9c8f9b-9de2-44fd-927d-45433c0dfc17`
- Task-SHA256 (Bridge content): `E33961BB113282A428556E8E9B6032A764C9D078C574D41FF6EA6592016DABAC`

## Closed in R4

### A — end-to-end transport
- Admin/seller remove-item and reorder-items carry expectedVersion + occurrenceKey endpoint → command → validator → composer → directory
- Story reorder carries per-row ExpectedVersions + occurrence; all-or-nothing
- AdminSetScheduleAsync calls EnsureStoredContentAuthorizedAsync
- Platform product CTA uses `IStoryProductTargetPort.EnsurePublishedExistsAsync` (Catalog validity); shop path keeps ownership
- Non-empty occurrence required on mutations (`story.occurrence.required`); create/add/submit/stop/return/composites fail closed without it
- FE `story-api.ts` + StoryManagementScreen transport version/occurrence on delete/reorder/composites
- all-route-matrix: no OPEN cells for issued mutation routes

### B — Media SaveChanges failure injection
- R3 misleading SaveChanges-failure test renamed to honest replay fact
- Recording store + SaveChangesInterceptor: pre-commit fail cleans attempt object only; after-save throw keeps durable asset+binding+occurrence and replays same asset
- Exact Postgres unique identity: SqlState 23505 + schema `media` + table `asset_owner_occurrences` + constraint `pk_asset_owner_occurrences`
- Ambient/caller transaction refused before object store (`media.story_upload.ambient_transaction_unsupported`)

### C — PostgreSQL rollback-only (tooba_alpha)
- C1 persisted Items + publication visibility + seller isolation (committedRows=0)
- C2 tenant-scoped occurrence duplicate 23505 + stale version zero-write
- C3 ambient-tx refusal + save-fail interceptor on relational Media context
- ZERO new migration; R3 occurrence PK immutable

## Proof summary

- Focused R4 + AMSC: **46/46** (Transport 4 + Media R4 2 + Relational 3 + AMSC 36; plus retained R3 media rename path in broader 20/20 earlier)
- Combined R4+AMSC final filter Pass 46/46; negative control: disabling RequireOccurrenceKey made `Remove_and_reorder_require_version_and_occurrence_on_owning_path` FAIL, then restored PASS
- `tsc --noEmit` 0
- Schema/migration this task: ZERO
- Live Story/Media business write: ZERO
- Host refreshed with `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1` (see RECEIPTS)

automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT
