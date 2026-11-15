PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1
Parent-Task: TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 09cbaa436a03310f0cb46e6e622824f938d6c728
Mode: REPAIR_QUEUE_ACTIVE_ARCHIVE_PARITY_SINGLE_READ_AND_REAL_DEFAULT_ORDER
Track: SHIPPING_OPERATOR_WORK_QUEUE
Title: Make employee queue rows, counts, blocked work and default ordering agree
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

ONE BOUNDED REPAIR
Fix the source-confirmed 271 read/query inconsistencies below. Preserve five job stages and Today/Future/All UI. No lifecycle/write change, new phase, broad refactor, new framework or speculative polish.
ONE worker / ONE active task. No delegation/subagents/concurrent task, historical-agent resume or self-issued follow-up. No screenshots requested/captured/generated/attached. No polling, broad test/baseline/stash campaign or loops.
Run named affected checks once after final code; rerun only affected failures after concrete correction. Finish one canonical Result and STOP.

ARCHITECT REVIEW — 271 PARTIAL, NOT ACCEPTED
Reviewed main 09cbaa436a03310f0cb46e6e622824f938d6c728, classifier, counts builder, query engine, actual query handler/policy, queue screen/columns and relevant test source. Existing worker proofs do not cover the actual handler/normalize/query interactions below.
1 ALL PAGE VS COUNTS: AdminFulfillmentWorkQueueCountsBuilder.Select -> InSelection -> MatchesStage accepts EVERY stage when selectedStage=all, including History; !IsActive(row) then returns true. But Build computes All/SelectedTotal from active only. A history row inside the classified set can be returned on the active All page while missing from its badges/total.
2 ARCHIVE BADGE DEPENDS ON VIEW: ClassifyAsync excludes FulfillmentStatus.Cancelled/Delivered unless selected stage==history, BEFORE projection/counts. Thus archive count is 0/missing on active view and changes when history is selected, even for the same search. Source status prefilter contradicts one shared complete physical row set.
3 BLOCKED ACTIVE WORK DISAPPEARS: ResolveStage ends with History for pendingDispatch=true group/no capability, and marks no-forward-code rows history without positive evidence that physical obligation is closed. A pending group whose permissions/payment/capture/source are blocked is not completed history. AvailableActionCodes==[] is not proof of no work.
4 CLAIMED ONE PASS IS TWO READS: QueryAdminFulfillmentWorkQueueHandler calls _query.QueryAsync then _query.CountAsync; BOTH independently call ClassifyAsync/load facts/_clock.UtcNow. There is no shared classification snapshot, so rows and counts can differ under a change/time boundary, and all reads happen twice. Sharing the builder's definition is not sharing the row set.
5 DEFAULT ORDER UNREACHABLE: AdminFulfillmentGridQueryPolicy.Normalize injects updatedAt desc when Sort empty. QueryEngine.ApplySort's overdue/job-time branch runs ONLY if sort missing. Actual default handler therefore bypasses the new priority ordering, despite a helper test being green.
6 ADVANCED FILTER SILENTLY IGNORED: NormalizeAdvanced accepts jobStage/timeScope through AllowedFields/OperatorsByField; EvaluateAdvancedAsync then calls ApplyFilterAsync, whose switch lacks these fields and defaults to the unfiltered source. They are NOT refused as reported. Do not silently accept a filter you cannot evaluate.
7 ZONE CLASSIFICATION: new job StateOf calls old AdminFulfillmentDispatchTimingAuthority.LocalDate, whose ResolveZone silently uses UTC on missing/invalid ids. New stage/time projection must not classify unknown-zone facts as an accepted local future/today date. Preserve old helper semantics outside this task if unrelated; repair the new classifier boundary.
Good base to preserve: exact physical WorkUnitKey, current row/plan binding, pure read-only job semantics, own-plan/first-dispatch distinction, one operator link, independent controls/legacy URL normalization and 64px existing grid.
No independent Architect runtime/test execution; no user acceptance or complete shipping claim. 270-R4 remains accepted within its bounded repairs. R2 backup audit and true concurrency limitation unchanged.
This is one cohesive query selection/count/default-order repair, not authorization to reopen shipping/payment/Returns architecture.

RECOVERY / READ FIRST
Fetch main safely; verify issued base exists and is ancestor of actual origin/main. Inspect compatible docs-only advancement; declare actual base. Unsafe code divergence/missing issued object -> RECOVERY_CONFLICT. No force/amend/reset/rebase/stash or unrelated scratch changes.
Read AGENTS/START-HERE, canonical BRIDGE-WAKE-V1 protocol/controller, backend/frontend feature skills and relevant feature standards. Follow Contracts/CQRS/Result/localization. No findings-register scan/audit/certification.
Read ONLY targeted 271 source/evidence and existing query/test conventions needed for these corrections. Do not repeat the previous task's broad discovery.

REPAIR A — ONE ACTIVE/HISTORY SELECTION WITH CONSISTENT BADGES
Active All contains ONLY active stages; explicit history contains ONLY terminal/no-obligation history and is time-independent. Use one shared exact row predicate for page selection, SelectedTotal, Undated and dimension counts.
Stage badges = active rows within selected time scope before stage; time badges = selected active stage before time; archive badge = whole terminal subset after common search/column constraints regardless of selected active stage/time. No terminal row in active page/total, no active blocked row in archive.
Load the common relevant owning facts before stage/time filtering; do not conditionally drop completed parent units just to populate active view. Preserve existing security/tenant/seller scoping. Terminal-vs-active decisions occur at physical row projection; a parent status cannot hide a distinct legitimate work unit or distort archive count.
Keep one package row / locked children suppression and exact group/parcel scopes. No duplicate history rows to inflate badges.
Derive "no work remains" from actual closed quantities/lifecycle/source obligations, not absence of offered capability. Pass only minimal existing physical facts into the classifier if current row fields are insufficient (remaining process/pack/allocation quantity, ongoing transit/received unresolved obligation). Do not invent domain states.
Blocked active group/parcel/package stays in an honest one-of-five stage with its existing precise reason, zero new permission/action. Distinguish actual fully allocated/settled returned source from a returned source whose next action is merely unavailable.
Do not default every unknown to preparation or active: use positive physical facts to distinguish terminal from blocked and record mapping changes compactly.

REPAIR B — ONE CLASSIFIED SET / ONE SERVER NOW PER HTTP RESPONSE
Add/reuse one narrow owning query method returning the existing page + counts from a SINGLE classification in one handler invocation. Existing result envelope/route can stay additive/internal.
Read injected server now ONCE for that classification; build filtered physical rows once; counts/selection/sort/page use the SAME materialized row set. Do not memoize across requests or cache mutable data as authority.
Keep existing narrow IAdminFulfillmentWorkQueueQuery interface evolution/consumers explicit; no second query engine, ambient request singleton, mutable cross-request cache or frontend count fallback.
If old QueryAsync/CountAsync methods remain for compatibility, the live handler must call the combined path once and not independently re-read. Demonstrate actual call path and exact upstream read-count/clock-count assertions; pure builder tests alone are insufficient.
This is a coherent read snapshot within the request, not a claim of a database serializable snapshot or cross-module transaction. Do not add locks/distributed transactions or relabel it atomic.

REPAIR C — REAL DEFAULT ORDER, EXPLICIT SORT PRESERVED
When the actual API request contains NO explicit sort, preserve that fact through Normalize/handler so overdue -> due today -> undated -> future, relevant JobTimeUtc and stable WorkUnitKey tie-break actually execute.
Differentiate user/saved-view explicit sorting from legacy injected defaults without guessing by matching updatedAt desc text. Do not treat a deliberately chosen updatedAt sort as absent.
Check frontend grid default request/saved-view behavior: clean entry must genuinely send no user sort or the documented explicit new job-priority sort. Persisted user sorts remain respected; no deletion/reset of saved views.
No sortable stage/time field accidentally allowed only because AllowedFields contains filter tokens. Whitelist sorts supported by the actual engine, with the existing explicit semantics.
Test real Normalize -> handler -> query path with conflicting UpdatedAt values so an overdue row cannot be accidentally sorted behind future work. Test explicit user sort as a separate positive case.

REPAIR D — UNSUPPORTED ADVANCED/TOKEN FILTERS REFUSE TRUTHFULLY
Reject jobStage/timeScope inside advanced conditions at normalization with existing stable validation error. Legacy queueFilter cannot silently become an unrelated SQL predicate or hidden stage authority inside advanced conditions; either explicitly reject unsupported form or implement a documented consistent form within the existing narrow contract.
Top-level selection remains documented one stage + one scope: validate supported operator/cardinality, do not interpret notEqual/multi-value input as FirstToken equality. Unknown token normalization may retain the safe documented default; unsupported operator/shape must not look applied.
Do not change ordinary supported search/column/advanced AND/OR semantics. No new expression engine.
Focused negative handler/policy tests must prove error before DB read, not just source comments. Existing valid stage/time and supported column expressions remain usable.

REPAIR E — INVALID/MISSING ACCEPTED ZONE IS UNKNOWN
At the NEW job-time classifier boundary validate accepted zone before day comparison. Missing/invalid zone must produce an explicit unavailable/undated state/label consistent with existing markers; keep active work visible under Today as an exception, not a falsely precise UTC-local classification.
Preserve stored UTC fact if needed for auditing, but no accepted local Today/Future assertion without a valid accepted zone. Do not silently substitute UTC/browser/Tehran. Exact known UTC is valid if actually recorded as accepted zone.
Keep DateOnly/instant/LTR display and half-open cutoff semantics for valid-zone facts. Do not rewrite old scheduling/timing policies or stored records.
Test missing zone, refused zone and valid UTC/independent-zone date boundary. No invented date/weekly schedule.

FOCUSED PROOF — ACTUAL QUERY/HANDLER, NOT ONLY PURE HELPERS
One compact new/expanded query/handler test class with controlled owning inputs or existing safe test persistence. Required cases:

SAME common set: active preparation, blocked active no-actions group, linked Created planning row, ongoing delivery, settled returned-history row and a truly delivered parent-history row. All page contains active only; history badge invariant when selecting a stage/time; explicit history contains only terminal rows.
Counts/page parity across page1/page2 and beyond page size, ordinary search/column/advanced filters, all/history selection; no duplicated package members; archive count doesn't depend on active selected tab.
Blocked active with actual remaining quantity + no actions stays visible; genuinely zero remaining/no obligation source remains history. No invented permission.
Actual combined handler invokes classification/fact readers/clock once; controlled source changing on a second read proves no second read occurs. No separate CountAsync query behind same response.
Request with no sort survives real normalization into job-priority ordering, with UpdatedAt intentionally conflicting; explicit updatedAt/user sort still works.
jobStage/timeScope unsupported advanced expression/operator/multi-value -> typed rejection before read; normal supported column expressions remain correct.
Invalid/missing accepted zone -> explicit undated exception visible Today; valid accepted-zone cutoff/day boundary unaffected.
Pure predicate/helper tests are useful but are not substitute for the normalize/handler/query wiring assertions.
Frontend: existing mounted queue test verifies page.total==selected count, no history in active All, history badge consistent across stage/time clicks, clean entry request does not smuggle updatedAt default, explicit saved sort remains, unavailable-zone label/unknown metadata honest.
Run only affected named 271 classifier/query/mapping/mounted tests and typecheck/changed builds + mandatory targeted feature checks once after final code. Do NOT rerun 270 lifecycle/replay/stock suites, unfiltered Fulfillment/Order/Shipping/Composition or legacy disposable-DB matrices.
No baseline comparison campaign. If mandatory guards fail, report relevant failure once; no git-show/stash loops or unrelated refactor to "certify baseline".

PROTECTED DATA / DEPLOY
Read projection/query/UI only. No changes to physical action authorization, source allocation/quantities, plan persistence/replay, payment/stock/hold/Returns/settlement/provider/notification policies.
No schema/migration/DDL, manual business SQL, new durable demo order/parcel/plan/hold/settings, cleanup/reset/reseed/delete/restore/backfill or fake live category to satisfy proof.
Current user records/draft and completed trial read-only. ONE canonical tooba_alpha5432 SingleStore/store-alpha FE3000->Host5088; no new DB/Docker/15432/15433/edition/connection/port/appsettings/key/CapturedPreview changes.
Use controlled test contexts/read ports; any unavoidable relational test data transactionally rolled back, never user rows or durable fixtures. State actual proof type.
Publish/restart only changed services once after checks, stop Host before overwriting locked publish output. Verify actual served BUILD_ID/chunk + health/ready once.
Read-only /fa/admin/fulfillments and /admin/fulfillments at390/1440 DOM/text/network without screenshots. Existing live undated set cannot prove live dated ordering; keep that limitation and don't manufacture data.

HANDOFF / RECOVERY
Persist exact TASK docs/ai/tasks/TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1/TASK.md.
Evidence docs/architecture/evidence/TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1/: TASK.md, RESULT.md, RECEIPTS.md, result-contract.txt, query-selection-count-parity-proof.md, default-sort-blocked-and-zone-proof.md, user-review.md.
Persist this 271 PARTIAL review at docs/architecture/evidence/TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271/ARCHITECT-REVIEW.md. Preserve original result; append corrections to source claims instead of rewriting history.
Update top AGENTS/START-HERE/Recovery pointer: latest accepted 270-R4, 271 PARTIAL, 271-R1 actual result awaiting review; active/none truthfully recorded; automatic next NONE. No broad docs cleanup.
Record the actual handler/query path, one-now/one-set proof, each source defect repaired, named checks/counts, live coverage/limits, changed paths, elapsed, code/evidence/final SHAs and origin parity.
One review URL to actual queue; no user/visual/functional/universal shipping acceptance. No extra implementation task.
Normal commit/push/fetch parity. No force/amend/reset/rebase/stash/unrelated files/secrets. Unsafe divergence -> RECOVERY_CONFLICT; compatible incoming docs normal merge with exact base declared.

CANONICAL RESULT / STOP
ONE complete final Result, no placeholder/probe/progress POST:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Actual Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row.
Status: actual PASS / PARTIAL / BLOCKED / RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 09cbaa436a03310f0cb46e6e622824f938d6c728
Base-SHA: actual verified work base
Code-SHA: actual implementation commit or NONE
Evidence-SHA: actual evidence commit or NONE
automaticNextImplementationTask: NONE
Body: defects/repairs, actual connected query/count/default-sort proof, active/terminal/zone semantics, targeted checks/deploy, protected data, paths/elapsed/finalHEAD/origin parity, review URL/limits.
Use real values/no unresolved placeholders. END_TOOBA_WORKER_RESULT then END_TOOBA_TASK, nothing follows.
PASS requires actual query wiring and tested counter/page parity, not comments or helper-only assertions. Worker PASS != Architect ACCEPT; no user/provider/financial/whole-shipping acceptance.
Complete lifecycle then IDLE and STOP; no automatic new task/repair/poll/wait loop.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK