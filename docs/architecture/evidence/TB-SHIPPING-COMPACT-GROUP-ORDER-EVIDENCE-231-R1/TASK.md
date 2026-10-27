PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231-R1
Parent-Task: TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Mode: LOSSLESS_OWNER_LONG_VERSION_AND_RECEIVING_FACT_CORRECTION
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
HARD35min / source22 / final13 focused provider proof+recovery+commit. One correction.
231 NOT ACCEPTED. No new live writer/HTTP/parity feature; no UI/runtime change.

FIRST
Read231 ARCHITECT-REVIEW/TOP and compact order-evidence decision.
Dedicated Chrome REVIEW window now belongs to user unsaved work: NEVER enumerate/automate/reload/close it.
Do not touch live Host/FE, data, account/cart, draft/pointer/assignment or preview settings.
Use ONLY existing isolated231 provider target/helper15433/order_tb231/tb231; do not relax it. Existing local
image only, no downloads/new environment framework. No shared migration apply.

FIX THE EXISTING VALUE/GROUP TRANSFER (not a parallel evidence subsystem)
A LONG TOKENS
SourceRevision and SelectionVersion must remain long end-to-end, SQLbigint, no cast/downconversion.
Keep supported format version1 and stable positive-token refusal. Prove values >int.MaxValue actually persist
and read back exactly through BOTH draft and confirmed group. No guarantee of incrementing max tokens here.

B FULL OWNER KEY
Replace optional responsibility-as-owner with a required Order-owned full owner identity:
Store(active trusted tenant key, no seller), Marketplace(no tenant/seller), Seller(real seller id,no tenant).
Reuse source authority semantics, no user id, no current-pointer owner inference, no invented tenant.
A compact owner-kind+nullabletenant+nullableseller is sufficient; no duplicated per-line ownership records.
No foreign Domain/Infrastructure reference. Own enum/value if needed; avoid type aliases/new owner framework.
Group's existing seller remains selling party. When owner kindSeller its key must match the group's seller;
Store/Marketplace must not be rewritten as that seller. Tenant key exact/Ordinal, not lowercased.
Validate coherent/all-or-none identity BEFORE attach/materialize. Legacy entire evidence null remains valid.
Missing owner for a non-null evidence is invalid; no heuristic upgrade/backfill.

C ACTUAL SELECTED RECEIVING FACTS, NOT TWO DATE-RANGE INVENTIONS
Reuse group's SelectedDeliveryDate/RequestedDeliveryDate as the chosen LOCAL receiving date.
Remove the newly invented ReceivingStartDate/ReceivingEndDate DateOnly range. Store actual opening/closing
TimeOnly bounds + method timezone for that accepted date; group chosen date required when evidence present.
Same-day positive interval required. Do not map min/max delivery days onto date-range endpoints or LeadDays.
Preserve fractions EXACTLY. PostgreSQL time/timestamp precision cannot represent every100ns TimeOnly tick:
if using typed TimeOnly fields, map their ticks to bigint (0..TicksPerDay-1) or another proved lossless scalar,
not microsecond truncation/rounding. No JSON/week copy. Test seven-digit fractional values explicitly.
Prevent existing ApplySchedule from leaving non-null evidence contradicting its chosen date/window: either
refuse incompatible change before mutation or invalidate only by an explicit supported group replacement
path. No stale evidence retained under a changed customer schedule. Preserve old legacy behavior with no
evidence. Reuse exact existing window representation where possible; no parsing guessed human labels.

D EXACT FINANCIAL FACTS / FIX THE FIXTURE
Refuse amounts unrepresentable in existing numeric(18,2) before attaching (extra nonzero fractional precision
or capacity overflow), never round/truncate. Validate accepted freight+packaging equals checkout+carrier
collect for this Manual captured slice (Free/funded/Carrier remain unavailable); guard overflow as a stable
refusal before mutation. Do not compute/replace an input side or invent a payer/funder.
Keep group currency/payable coherence; do not change Payment/Settlement semantics.
Historical replacement fixture must use a COHERENT new accepted quote and matching new draft group amount,
not150000+7000 vs125000+0. Then prove old stored order facts stay unchanged.
Optional min/max customer delivery days must obey actual accepted domain bounds and remain metadata.
No recalculator, no runtime quote call from Domain.

E MAPPING/MIGRATION HONESTY
Update existing owned mapping and the231 migration to these corrected scalar facts; source/task changes
are still unshared/unaccepted, so correcting THIS231 migration in place is explicitly authorized here.
Do not edit any earlier/shared migration, add redundant migrations to avoid fixing an unaccepted schema,
or rebaseline unrelated entities. Fresh isolated DB applies normal chain once.
Do not silently adopt same-named wrong-type columns/index via IFNOTEXISTS. Fresh additive columns should
fail if contradictory objects exist; no guessed schema repair.
At minimum persist ALL owner/version/hour/amount fields losslessly and enforce complete-vs-absent evidence
storage plus basic invalid shape checks on both existing tables, or implement an explicit validated read
that refuses partial/corrupt persisted values. Never let a partial row hydrate into an apparently complete
success. Do not describe constructor-only tests as database all-or-none proof.
Current EF snapshot is pre-existing stale and lacks these group entities. Do NOT regenerate the entire
snapshot. Make a scoped synchronization of touched group/owned mappings only if feasible without unrelated
baseline rewrites; otherwise retain/document the baseline gap and prove runtime model vs migrated actual
columns/types/nullability/indexes for these touched tables. Do not claim snapshot/future scaffolding clean.

FOCUSED PROOF ONLY
Extend exact231 fact classes and real provider fixture:
- low and >int.MaxValue tokens exact, all three owner forms preserved; owner mismatch refused unchanged;
- two groups same shared snapshot, distinct row/area/financial/selected date+fractional hours intact;
- later coherent draft replacement leaves actual stored order owner/token/money/date/hour unchanged;
- altered selected schedule cannot coexist with stale evidence;
- extra fractional money/conservation mismatch/partial owner rejected BEFORE any mutation; explicit0 valid;
- incomplete persisted evidence rejected by actual DB checks OR validated reader, not defaulted;
- legacy null evidence still works; scalar/noJSON storage with actual new column count reported, not19byhabit.
Run one focused class selection + directly affected materializer facts; changed Order builds only.
No fullOrder/Shipping/119/188/194/Host/solution/guard sweep. Provider identity asserted before DDL and own
container removed/port verified afterwards. No live-runtime/table changes.

FINISH
Evidence docs/architecture/evidence/TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231-R1/{TASK.md,RESULT.md}.
Append correction to231RESULT preserving history. Update ALL THREE TOP recovery files with actual result,
migration corrected WRITTEN/sharedNOTAPPLIED, exact owner/version/time fidelity proof and baseline limits.
Keep user read-test window/runtime unchanged and not_ready guard. Next only after review: trusted server
selection/commit revalidation writing this evidence and storage-only historical reader, not before.
Known read-only UI/cross-owner-label/decoder/OfferAPI gaps stay recorded, no claim fixed by storage.
Exact-path commit/push/fetch parity, no add-A/history rewrite/Bridge/post-result scripts.
Record actual elapsed/partial blockers truthfully; STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
