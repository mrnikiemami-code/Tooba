# Architect review — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11
Date: 2026-10-22
Reviewed main: 3fef1455ae12ca612ced7ac4720e7c679e6206c1
Substantive commit: 2a60ddd2d421a8b5bd6ec630c6914015c7ab5af1
Verdict: ACCEPT bounded existing-CHECK/name mapping alignment.
Whole196: BLOCKED_PENDING_DEFAULTS_AND_RELATIONAL_ACCEPTANCE.

## Independent source/evidence review
Compared 8aaf599..3fef1455. Runtime changes only the two historical CHECK annotations and ten existing physical names; all shape expressions, column types/nullability/defaults/value generation and FK action stay unchanged. Only current snapshot/latest R10 Designer generated metadata updated; older designers and ALL executable migration files absent from diff.
Read both new tests and parity-test update. Exact checks/ordered index columns/unique/filters/FK principal/delete action asserted, comparer negatives for rename/missing/relaxed check exercised in source. Latest-migration selection now follows actual migration universe with expected R10 assertion; original type-specific checks retained. No forced rewrite of historical type TargetModel.
Independently computed differences of BOTH complete saved manifests: primary218/model218; exactly10 differing lines identical to saved residual-default-lines.txt. These are five 0/empty-string defaults vs absent defaults; no residual name/CHECK/index difference in those files. This is a saved pg_catalog dimension comparison, not proof of all possible provider facets or a live merchant DB.
Worker reports6 mapping/parity +5 offline R10/factory +1 residual PostgreSQL facts passing and zero build errors, with one compile-harness correction. Architect reviewed remotely, not independently executed or timed these tests/cleanup.

## Proof limits to retain
Residual test's classifier matches the counterpart by column-prefix and proves exact primary-side defaults, but does not assert the full counterpart default text. Saved manifests independently show '-' for all five, so CURRENT result is sound; don't advertise the helper as detecting every future default mutation.
History exclusion is string-based with a table-name prefix, despite the exact-identity comment. Current files show only intended history objects excluded; this is not a general exact-table guarantee. Next authorized residual-parity test must exclude history by exact schema/table identity in SQL and compare entire remaining manifests without classification/removal/ignore.
No need to rerun R11 or open a repair wave solely for those limitations; the next transition to zero differences makes the stricter assertion natural.

## Exact default decision / sole next task
TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R12, parentR11: Describe the FIVE historical server defaults with correct typed HasDefaultValue and EXPLICIT ValueGeneratedNever, preserving prior EF explicit-write behavior. This is NOT blanket HasDefaultValue/ValueGeneratedOnAdd approval. No production domain/API change and no DDL/default removal/new migration.
Only five existing property configurations, current snapshot and latest R10 TargetModel may change. All executable migrations, older designers/factory/shared persistence immutable.
Require real EF INSERT SQL/parameter proof before/after on disposable PostgreSQL: zero/empty AND nondefault values still explicitly sent; metadata ValueGenerated.Never and Save semantics retained; NEW-context stored values unchanged. Persisted historical defaults remain available for raw omit-column SQL exactly as before, not newly introduced business fallback. Explicit-null required strings must not be silently defaulted.
Full real-chain schema vs model projection must now compare EXACTLY equal without default/name/check exemptions. Real initialized snapshot/newest target parity must remain zero. Update only the prior five-default residual assertion to the stricter zero-difference contract, retaining honest prior history in evidence.
No full-schema fixture redesign/storage/race battery this task; that old fixture still replays a pre-R10 chain and will need separately scoped chronology-aware integration proof. Do not falsely claim whole196 closure when standalone parity goes green. No assignment/UI/quote/deployment;TB185paused.
