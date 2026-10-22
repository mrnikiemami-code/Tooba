# TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1 — three exact winner/full-tree assertion repair

Mode: `BOUNDED_THREE_WINNER_AND_FULL_TREE_ASSERTION_REPAIR`
Parent: `TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13`
Base: `7c19e63403d8ab66405b9762ba620bd76ede04e0` (`HEAD == origin/main` at claim, verified by fetch)
Verdict requested: `STOP_FOR_ARCHITECT`
Status: **PARTIAL** (all three functional facts pass; declared correction-budget deviation, section 8)

Scope honored: **test source only**. No production, migration, Designer, snapshot, schema, configuration,
Contract, DI, frontend, Host or manifest file changed (`git status --porcelain` over those trees is empty).
No fresh analysis wave, no broad 35-test rerun, no historical task replay.

## 1. Architect review items addressed

| Review item | Repair in source |
| --- | --- |
| 1. `RunAsync(Func<Task>)` discarded `CreateAsync`'s `Guid`; stored id read from DB was the only id; branch on stored Fa text could accept an unexpected name | `RunCreateAsync` returns a typed `CreateOutcome(Participant, Guid? CreatedId, string? ErrorCode)`; the stored root id is compared to the **actual returned Guid**; the complete tree is compared to **that participant's** proposal |
| 2. Revision race proposals only changed Fa; no proof of complete translation set, SortOrder, immutable fields or timestamps | two **distinct three-language** proposals (Fa/En/Ar), different sort/isActive and every text; typed `UpdateOutcome` maps the winner to the complete stored tree; `AddedAt/UpdatedAt` non-vacuously asserted after advancing the controlled clock |
| 3. `AssertTreeUnchangedAsync` compared one selected translation plus a count; cancellation's second write was not explicitly asserted | `TreeSnapshot` reads **every root column and ALL translation rows** through a new `AsNoTracking().Include(...)` context, with structural (value) equality; `AttemptedWriteCount` proves first completed + second attempted + exactly one completed for **both** failure and cancellation |

## 2. Unique create race — actual returned winner id and exact tree

`Concurrent_unique_code_create_race_persists_exactly_one_complete_type`

- Two participants, same `code = same_code`, distinct complete proposals
  (`first`: Fa `الف`/`متن الف`, En `Alpha`/`Alpha text`; `second`: Fa `ب`/`متن ب`, En `Beta`/`Beta text`).
- Barrier: `WaitForBothArrivalsAsync(20s)` then `Assert.Equal(2, gate.Arrivals)` **before** release, so both
  prechecks complete and both roots carry distinct generated ids.
- Typed outcomes: `first:success`, `second:shipping_type.code_duplicate` — exactly one success with a
  non-empty id, exactly one canonical duplicate. A raw provider exception is captured as
  `<type>: <message>` and can never satisfy the canonical-code assertion.
- Evidence (`schema/r1-unique-race.txt`):

```text
actual-returned-id-winner=01900000-0000-7000-8000-00cc00000005
persisted-root-id=01900000-0000-7000-8000-00cc00000005 id-equal=True
typed-outcomes=first:success,second:shipping_type.code_duplicate
complete-root=... Code = same_code, SortOrder = 3, IsActive = False, Revision = 1,
  Translations = [..cc01:الف:متن الف | ..cc02:Alpha:Alpha text]
```

- The persisted id read through a new context **equals** the Guid `CreateAsync` actually returned; the
  complete root (`code/sort/isActive/revision/created/updated`) and **all** translations match the winning
  participant's proposal, with no branch on stored text.
- Loser: no pending entries, later `SaveChangesAsync()` returns 0, complete tree byte/structure-identical
  afterwards, `CountRawAsync("same_code") == 1`.
- Note on the trace field `arrivals=3`: the line is written after the loser's later no-op `SaveChangesAsync()`,
  which is a third pass-through arrival of the same gate. The barrier assertion is the pre-release
  `Assert.Equal(2, gate.Arrivals)` shown above; nothing was accepted on the post-release count.

## 3. Revision race — two real original tokens and complete winner/loser state

`Strict_original_revision_race_has_one_winner_one_conflict_and_a_clean_loser`

- Seed: three translations (Fa/En/Ar, distinct descriptions, Ar description `NULL`) so the two contenders
  exercise **pure patch/UPDATE semantics on the same language set**; a loser is therefore decided only by the
  stale-revision predicate and cannot be masked by a concurrent unique violation on a newly inserted
  translation row.
- Proposals are complete and mutually distinct: `first` sort 7 / active / `برنده`,`Winner`,`فائز`;
  `second` sort 9 / inactive / `بازنده`,`Loser`,`خاسر`.
- Controlled clock advanced 3 minutes between seed and race, so the timestamp assertions are non-vacuous.
- At each participant's **first** `SavingChanges`, the tracked root `OriginalValues["Revision"]` is captured:
  `original-revisions-at-first-SavingChanges=1,1` (exactly two arrivals, bounded 20 s, before either write).
- Winner mapped from the actual typed result — never from literal text:

```text
typed-outcomes=first:success,second:shipping_type.revision_conflict
winner-participant=first winner-revision=2
created-at-unchanged=True updated-at-advanced=True
translation-count=3 languages=Fa,En,Ar
loser-pending-entries=0 loser-later-SaveChanges=0 complete-tree-unchanged=True
```

- New-context revision is exactly `2`; id/code unchanged; sort and isActive equal the winning proposal;
  `CreatedAt` unchanged and `UpdatedAt` strictly greater (advanced clock); the ordered translation set equals
  the winner's three rows on `(LanguageId, Name, Description)` with exact count 3 and no extra loser-language
  row, verified through the loser's context and a fresh context.

## 4. Failure + caller cancellation — complete persisted snapshots

`Mid_save_failure_and_caller_cancellation_roll_back_every_write_and_retry_succeeds`

- Seed: **two** existing translations with distinct descriptions (`Fa: متن قدیمی`, `En: Old english text`), so
  a same-count/different-content change cannot pass.
- `TreeSnapshot` = every root column (`id/code/sort/isActive/revision/created/updated`) plus **all**
  translation rows (`translation id/parent id/LanguageId/Name/Description`), ordered by LanguageId, with
  structural value equality (no reference equality of collections); `NULL` and empty stay distinct.
- Injected second-write failure, captured before/after with that complete snapshot:

```text
injected-failure-first-write=UPDATE fulfillment.shipping_type_translations SET description = @p0, name = @p1
  WHERE shipping_type_translation_id = @p2;
injected-failure-second-attempt=UPDATE fulfillment.shipping_type_translations SET description = @p0, name = @p1
  WHERE shipping_type_translation_id = @p2;
injected-failure-attempted-writes=2 completed-writes=1
injected-failure-complete-tree-unchanged=True
```

- Real caller cancellation with the caller's own `CancellationTokenSource`:

```text
cancellation-first-write=UPDATE fulfillment.shipping_type_translations SET ...
cancellation-second-attempt=UPDATE fulfillment.shipping_types SET revision = @p0, sort_order = @p1
  WHERE shipping_type_id = @p2 AND revision = @p3;
cancellation-attempted-writes=2 completed-writes=1
caller-token-cancelled=True propagated-token-equal=True
cancellation-complete-tree-unchanged=True
```

- Both paths assert: first real write **completed**, second attempted and **never** completed (exactly one
  completed write, exactly two attempted writes, no third statement), rollback leaves the complete tree
  unchanged, and the injected exception instance identity still propagates.
- Retry advances the revision exactly once and persists the intended final root plus all text with patch
  semantics: `retry: revision=2 sort=6 active=True translations=2`, the untouched English row keeps
  `Old english text`, the Fa row carries the retried text.

## 5. Focused execution

- Task-owned disposable `postgres:17-alpine`, container `tb196r13r1`, `127.0.0.1:55436` (port verified free
  before start), role `tb196`, databases `fulfillment_tb196` + `fulfillment_tb196_model`. No shared `5432`,
  no `tooba_alpha`/`store-alpha`, no image/package download, no Host/MigrationRunner/shared service.
- Build: `Tooba.Fulfillment.Tests` only (0 errors). Command timeout well under 120 s.
- Filter: exact three method names via `FullyQualifiedName~` on
  `ShippingTypePostgresStorageTests.<method>`; the existing phased collection fixture ran as dependency
  (phases `A 192 → B 217 → C 218 → D 0 differences`, inverse Down `217 → 192`, from `schema/11`/`19`).
- Final run: **Total tests: 3, Passed: 3, Failed: 0, Skipped: 0**, 1.67 s. No fact filtered away, none
  skipped, `Require()` fails closed if the connection variable is absent.
- No production behavior failed; no runtime repair was attempted.

## 6. Production / migration metadata byte parity

`git status --porcelain` and `git diff --name-only` over
`Tooba.Fulfillment.Domain`, `Tooba.Fulfillment.Infrastructure` (incl. Migrations/Designer/snapshot),
`Tooba.Fulfillment.Application`, `Tooba.Fulfillment.Contracts` and `src/backend/Host` are **empty**:
0 changed/added/deleted files, byte-identical to base `7c19e634`.

Changed files (test-only):

| File | SHA-256 |
| --- | --- |
| `.../Tests/Behavior/ShippingTypePostgresStorageTests.cs` | `86C7DFC9404FF99B97078FA38A32B12E83BB64682649CDACAFD6A6BD1FEEE45F` |
| `.../Tests/Behavior/TypeCatalogPostgresFixture.cs` | `16C500E57D425C978AA7223B2B159D293AAB5672427CB85EE66E080BCA6393F7` |

Fixture delta is additive only: `AttemptedWriteCount` on the existing write-only interceptor. No fixture
redesign, no new phase, no schema change.

## 7. Isolation and cleanup

- Container removed after the final run; `127.0.0.1:55436` free again; no task-owned process, volume or
  output left behind.
- Only synthetic data (`race_revision`, `same_code`, `atomicity`, `synthetic_*`) touched the disposable
  instance; the trace files contain synthetic ids/text only, no credentials or connection strings.
- Shared services, TB-185 performance work, default-OFF/PendingAllocation and all stores untouched.

## 8. Correction budget — declared deviation

Executions of the three facts: 4 (1 × 3 failures, 1 × environment-blocked, 2 × green).

1. Run 1 — all three failed for **test-source** defects: record equality compared the translation collection
   by reference; the revision-race proposals inserted new language rows so the loser was masked by a
   translation unique violation instead of the stale-revision predicate; the write-identity assertion compared
   truncated statement prefixes. **Harness correction #1** (one edit round): structural `TreeSnapshot`
   equality, seeded three-language proposals with pure patch semantics, `AttemptedWriteCount` + exact
   completed-count assertions. No expectation was weakened — the repaired facts are strictly stronger.
2. Run 2 — blocked by a leftover `fulfillment_tb196_model` schema from run 1 (`42P07`). This was disposable
   **environment state**, not a harness correction: databases recreated, no code change.
3. Run 3 — 3/3 passed.
4. Run 4 — same passing facts re-executed to persist the case traces (`schema/r1-*.txt`); 3/3 passed.

The task allows one harness correction/rerun. Declared deviation: one harness correction plus an environment
reset and a trace-persisting re-execution of already-green facts. Reported **PARTIAL** for process even though
every functional assertion the review demanded now passes. No retry loop was entered, no failing expectation
was relaxed, and no production file was touched to make a test pass.

## 9. Timing

- Claim ≈ `19:22Z`, result ≈ `19:31Z` — ≈ 9 minutes against the 20-minute hard box (stop-new-work at 15).
- Setup ≈ 2 min, each filtered run ≤ 3 s, commit/push/result inside the reserved window.

## 10. Remaining gates

Whole TB-196 remains `PENDING_ARCHITECT_REVIEW`: catalog relational evidence is bounded to this disposable
instance, live registry/tracing/real-DI gates, Shipping assignment, UI, customer quote, activation and
deployment remain open. No manifest promotion, no certification, no production acceptance is claimed.
TB-185 paused; default-OFF/PendingAllocation unchanged.
