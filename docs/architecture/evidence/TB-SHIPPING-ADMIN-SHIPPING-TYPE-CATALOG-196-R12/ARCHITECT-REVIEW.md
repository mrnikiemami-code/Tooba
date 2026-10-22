# Architect review — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R12
Date: 2026-10-22
Reviewed main: e4fdcf587facb9bef1d28045c00744bbffb17244
Substantive code: e604e2704c50c5c70756ec5e0ba465928a225a91
Worker verdict PARTIAL retained as process history.
Architect verdict: ACCEPT bounded five-default modeling/current-schema parity with explicit corrections and limits below.
Whole196 remains BLOCKED_PENDING_FULL_RELATIONAL_ACCEPTANCE.

## Source and evidence
Compared49f5949..8284c1f; later e4fdcf adds result-contract.txt only. Runtime diff is the five authorized mappings with ValueGeneratedNever (plus BOM); metadata adds only corresponding default annotations. Executable migrations/older designers/factory/business files absent from diff. Strict manifest test now excludes exact history schema/table at SQL level for columns/constraints/index owning tables and asserts full equality without classifier.
Independently read both saved complete manifests: byte-identical218lines each. Read captured pre/post evidence and the insert-test source: real EF SaveChanges interceptor, explicit column/no DEFAULT checks, nondefault fresh-connection values, NULL rejection, raw omit behavior. Ten case-result lines match baseline/after; this is useful bounded ORM behavior evidence, not a domain-validation claim. Worker reports8passed/no failures, not architect reruns. Model parity relies on real initializer/differ with no ignore.

## Explicit corrective decision
Ratify ONLY the two decimal HasDefaultValueSql("0") annotations rather than typed0m to preserve the historical SQL expression, still followed by ValueGeneratedNever. The constant is module-owned static storage metadata; no external SQL input/new policy. No requirement to change literal0 to0.0 or hide the expression diff; other three defaults remain typed. This ratifies a reported scope deviation after review; not permission for future workers to substitute mechanics without stopping.
Four harness corrections plus model-DB reset exceeded the one-correction rule. Process noncompliance remains; technical acceptance is not retroactive compliance. No repeat of accepted metadata/schema work solely to relabel PARTIAL.

## Evidence limits / narrowly required strengthening
Saved insert files contain case summaries, not actual SQL or column-to-parameter bindings; value-membership assertion checks any parameter rather than that column's bound parameter. Nondefault readback plus explicit-column and Never/Save metadata supports bounded behavior, but do not claim full SQL byte-for-byte equality from these summaries. AssertExplicit runs only inside verify+evidence branch; make current-model invariant unconditional in focused proof. The new task may harden this TEST proof without production edits or recreating old baseline.
R12 did not add the required same-history-prefix negative, and existing CHECK/name test does not test a changed default (contrary to prose). New exact zero equality source is sound for current manifests; keep the missing negative as unproved until executed in next test slice.

## Source diagnosis for next relational stage
Existing TypeCatalogPostgresSchemaFixture still gates current model against chain ending BEFORE ShippingTypeCatalog/R10; current index can never appear there. Storage raw SQL/ReadRaw helpers still use PascalCase despite accepted snake_case migration. TwoArrivalGate does not record original root token per context, and StatementFailureInterceptor counts SELECTs as completed statements. These are concrete stale-test/evidence defects, not production defects; repair only in tests before claiming PostgreSQL proof.
Sole next task TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13, parentR12: chronology-correct staged migration fixture, exact schema0diff gate, actual storage/constraints, strict original-token races, write-only mid-save failure/caller-cancellation/retry and exact-owned-constraint negatives. Test/evidence ONLY, all production and migrations byte-identical.
Keep actual historical type-additivity proof and R10 index-delta/Down proof; don't erase old invariants to bypass chronology. Current-chain strict equality must precede storage, no count-only / classifier loophole. Missing target fails closed, no skips. If real production behavior fails, STOP/BLOCKED with exact reproducer; no absorbed production repair.
No whole-module recertification/release, owner Shipping assignment/UI/quote/activation/deployment;TB185paused.
