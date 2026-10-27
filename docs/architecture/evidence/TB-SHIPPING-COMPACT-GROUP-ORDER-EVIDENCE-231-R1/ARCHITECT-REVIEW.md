# Architect review — TB-231-R1 — 2026-10-27

Reviewed fe5e3d8e61ee07af87de552026c655b4ff916f9e against5cd72434ebe68bc28c2ee214a10f8c62ee2d2017.
Verdict: corrected VALUE/TRANSFER accepted within focused proof; migration/storage-shape acceptance PENDING
the narrow231-R2 correction below. No captured checkout or live writer acceptance.
Architect read full value/owner, migration CHECKs, group attach/schedule/materialization checks, mapping
and provider tests. Root did not execute Windows suites; worker50focused/provider facts remain worker proof.

Confirmed corrected facts: long/bigint tokens; required Store/Marketplace/Seller key; exact100ns ticks +
accepted local date/timezone; representable numeric18,2 and conserved manual amounts; coherent replacement
fixture; legacy null evidence; Order-owned scalar value/noJSON/no foreign FK. Existing private order value
and group-transfer path retain compact storage direction. Broader stale EF snapshot was disclosed and scoped
model-vs-DB comparison is useful; future scaffolding is NOT certified.

SOURCE-CONFIRMED SQL COUNTEREXAMPLES not covered by the8 raw updates:
Current optional-day CHECK:
(min IS NULL AND max IS NULL) OR (min>=0 AND max>=min AND max<=365).
For minNULL/max3 or min2/maxNULL the expression is UNKNOWN, which PostgreSQL CHECK accepts.
This repeats the SQL three-valued-logic pitfall already corrected in the earlier Shipping191 lineage.
The source constructor rejects half pairs but EF materialization bypasses Create; DB claim is therefore false.

Other cheap canonical-shape holes in SAME claimed DB protection:
- Store tenant""/spaces passes ISNOTNULL, though domain Store requires nonblank.
- Required UUIDs and optional-present type/owner seller may be Guid.Empty; only nonnull is tested.
- currency123 or A$! passes char_length3 despite domain ASCII letters requirement.
These are NOT new product rules; make the existing basic storage-shape claim truthful.
No more value/owner/time design changes required. Do not reopen the accepted application contract.
231-R2 changes only pending migration checks and direct provider facts, preserves22columns/model/transfer.
Migration231 remains sharedNOTAPPLIED; narrow correction in place still authorized. No shared history rewrite.
No Domain/UI/runtime change or user-review-window access. not_ready stays until later trusted writer/parity.
