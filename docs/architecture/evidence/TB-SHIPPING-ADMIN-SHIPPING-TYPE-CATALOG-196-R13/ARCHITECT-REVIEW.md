# Architect review — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13
Date: 2026-10-22
Reviewed main: ad7d7a3c77d55a39358137d63e9d01e25f78aba4
Substantive test/evidence commit:86c698fe251a1a45095205b7f9dcb272bae866a2
Verdict: ACCEPT phased schema/storage/constraint/default-trace improvements within observed scope; REPAIR_REQUIRED for three overstated winner/full-tree assertions.
Whole196 remains PENDING_ARCHITECT_REVIEW / not fully accepted.

## Independent review
Compared563f033..86c698: tests/docs only, no production/migration/model change. Laterad7d7a adds result-contract.txt only. Read fixture, storage tests, result and phase manifests. Independently byte-compared model218/current218 and pretype192/afterDownType192: equal. Actual logged phases192->217->218->217->192 support chronological Up/Down with zero current-model differences; no historical-schema exception needed.
Fixture now uses actual snake_case, strict latest-model gate, exact type ownership and write-only failure interceptor. Original tracked revisions are captured at SavingChanges and both asserted1. Conflict/unique outcomes require a single failure from two participants, so exactly one success follows. Real caller CTS is cancelled and its token propagated; actual first completed write precedes injected second failure. These are meaningful improvements. Constraint-lookalike/wrong-state assert same exception instance.
Reported35passed and cleanup/timing are Worker evidence; architect did not rerun them. Two harness corrections exceed the one-correction rule: process noncompliance remains. Don't re-run accepted work merely to change PARTIAL into PASS.

## Three concrete unclosed proof requirements
1. Unique race: RunAsync accepts Func<Task> and returns only ObservedFailure; CreateAsync's Guid result is discarded. Source only prints the id read from the database, despite report saying "winning id captured from real result". It does not compare the stored complete translation pair to the ACTUAL successful participant; its conditional based on stored faName could accept an unexpected name in one branch. Need typed result capture and exact winner proposal.
2. Revision race: proposals update only Fa; source checks that name/description, IsActive and revision. It does not assert full translation set/no extra loser language, SortOrder, immutable root fields/timestamps or complete winner state claimed in prose. Need two distinct multi-language proposals, exact original predicates, actual winner mapped to complete stored tree and exact counts/ids.
3. Rollback: AssertTreeUnchangedAsync compares RawRow(revision,timestamps,ONE selected translation,count). ReadRawAsync doesn't include code/sort/isActive/all translation ids/text; same count can conceal different other-language content. Need complete persisted root+all translations snapshot and equality after BOTH failure and cancellation, before retry. Cancellation's second attempted/noncompleted write must be explicitly asserted, not only first-write boolean.

Sole next task TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1, parentR13: repair ONLY these three facts/helpers and append evidence corrections; no new feature/runtime change/schema evolution. Existing phase gate executes as fixture dependency, but no broad35-test confidence rerun. Existing accepted raw storage/schema/default facts reused by exact source identity.
No production failure identified and no production repair authorized. Do not claim catalog closure before this precise proof is reviewed. No Shipping assignment/UI/quote/deployment/certification;TB185paused.
