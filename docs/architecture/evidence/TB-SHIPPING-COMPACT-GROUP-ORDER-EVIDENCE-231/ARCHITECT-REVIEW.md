# Architect review — TB-231 — 2026-10-27

Reviewed main ded6ad91d4a08ff630e2f7137160c028a0fe0e98, compare against5a3070567edb8806db95e50b18bfa4f5c6a58100.
Verdict: NOT ACCEPTED for compact evidence contract/storage. Direction is correct (bounded scalar value
on existing groups, no authored JSON, real materializer/provider), but required facts are lossy/incomplete.
Architect read domain/value/group changes, mapping/migration, provider tests and current model snapshot.
No local tests executed by Architect; reported21+9 and builds are worker execution.

Blocking source facts:
1 SourceRevision and SelectionVersion use int/integer. Shipping219/216 exact tokens are long. Values above
  int.MaxValue cannot round-trip. No narrowing/cast/rounded token may be required by future writer.
2 No full shipping owner identity is stored. Optional Seller/Platform responsibility plus group.SellerPartyId
  does NOT distinguish Store tenant / Marketplace / shipping Seller key. Selling party is not always owner.
  Current value accepts responsibility null despite claiming complete evidence. SnapshotId alone must not
  cause a future reader to guess owner from current pointer/user/edition.
3 ReceivingStartDate/ReceivingEndDate are DateOnly dates, not actual customer chosen interval hours.
  Existing group date/window fields are reused only nominally: evidence attach/materialize checks currency
  and payable amount, not date/time/owner coherence. Current tests permit a 2026-10-31..2026-11-07 date range
  for a promise min2/max3. That is invented semantics, not stored accepted09:00–17:00 or exact fractions.
4 Value accepts amounts not exactly representable by numeric(18,2), and no conservation check exists for
  freight+packaging vs checkout+carrier collect. Provider fixture replaces freight with150000 and packaging
  with7000 while leaving payable125000/carrier0, then calls it a correct changed price. That is incoherent
  accepted financial evidence, not a valid historical-pricing test. Refuse, do not recalculate any side.
5 Model snapshot contains NEITHER CartShippingDraftGroup nor OrderShippingGroup (pre-existing baseline drift).
  Its unchanged state is not proof of schema/model equivalence. New migration uses ADD COLUMN IFNOTEXISTS,
  so a same-named wrong-type object could be silently adopted. Do not claim future EF migration safety.

What is demonstrated: successful provider storage for chosen low-version fixtures, scalar/noJSON shape,
distinct group/row ids and copying via real materializer, legacy absence, independence of copied order
from later draft object. It does NOT prove full owner/history/date/long-token fidelity or arbitrary incomplete
DB-row rejection. Source-level Create invalid cases are not SQL all-or-none constraints.

231-R1 fixes THIS unshared/unaccepted value and mapping, not checkout/parity/UI. Migration231 has not been
shared-applied; correcting it in place is explicitly allowed only here before shared deployment. No historical
shared migration rewrite. Existing broader stale snapshot baseline is not to be regenerated indiscriminately.
The user's dedicated visible review window has been handed off and MUST NOT be automated/reloaded.
230/R1 read-only visual acceptance remains; current not_ready guard/runtime/data unchanged.
