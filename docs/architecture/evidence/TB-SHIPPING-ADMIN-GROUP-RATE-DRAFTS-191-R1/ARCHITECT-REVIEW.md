# Architect review — TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1
Date: 2026-10-22 (Asia/Tehran)
Verdict: ACCEPT — BOUNDED_GROUP_RATE_DRAFT_PERSISTENCE_THROUGH_R1
Reviewed HEAD: 7588dd80d2f916dc35b46fd7f0e0bea7efeb4073
Repair code: 7fc3b0bc6352cadd6be3a2290ef55ef7dcd1c44d
Lineage: TB-191 (historical PARTIAL/process deviation) -> TB-191-R1

## Closure of requested repairs
Source/diff review confirms the five current-model CHECK corrections have one new migration and designer; original TB-191 and older migration files are not in the diff. Explicit min IS NOT NULL closes the nullable interval branch, minimum merchandise amount is capped, and currency is NULL or three ASCII uppercase bytes/letters under C collation. Down preserves historical rollback expressions rather than rewriting history.
The raw storage regression bypasses aggregate/transport, asserts PostgreSQL 23514 and owned constraint identity in isolated per-attempt transactions, and retains valid both-null/min-only/adjacent/at-limit values.
Two new tests run rate replacement versus region replacement (retention/edit and removal of the rate-owning group). They use the existing bounded pre-write two-reader barrier, accept either winner, verify winner-dependent tree/rate state and canonical losing revision conflict, check actual loser tracking and no staged/orphan rows.
No new executable business/Domain/HTTP/permission/owner/currency policy was introduced. Only storage mapping/migration and focused tests/docs changed.

Worker reports relational 24/24 and whole Shipping 577/577, zero fail/skip, cleanup and nine-minute execution within the 35-minute bound. Architect independently read source/diff/evidence; did not execute .NET/PostgreSQL here. No production database proof/application is claimed.

## Acceptance boundaries and history
Accept TB-191's persisted manual row slice as corrected by R1: group-only geography reference, AND/full-tuple half-open conditions, independent freight/packaging/duration, trusted bound currency, authorized reads/writes and shared root revision.
This does not change TB-191's historical worker PARTIAL, forbidden Host.Tests run or hard-deadline breach. The 132 Host failure attribution remains UNVERIFIED_NO_HOST_RERUN; no comparison or Host closure is inferred.
Runtime group valuation/discount/tax/measurement gate remains OPEN_NOT_IMPLEMENTED. No customer quote/activation, frontend/editor or deployment acceptance.

## Next single bounded task
TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192 (parent TB-191-R1): owner-local display name/description for an existing method draft and optional group titles, keyed by authoritative database language IDs; shared Fulfillment catalog identity/translations remain untouched.
One active task only, no automatic continuation. Read recovery/actual main before execution. TB-185 remains paused.
