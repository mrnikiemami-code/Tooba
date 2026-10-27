# 232-R2 Architect review — ACCEPTED within captured DRAFT-selection scope

Reviewed source98c1c4ee0738bba8ede5eadddfb1f4179a48852e and actual adapter, transaction tests and TransactionProbe on2026-10-27. Root did NOT execute the Windows/PostgreSQL tests;31/31/builds/RED disposal fact are worker-reported execution corroborated by test source.

## Accepted
- Both owned transaction paths now await finally disposal; failure cleanup consumes/disposes before recovery read and nulls local handle, successful paths dispose in finally.
- Caller EF CurrentTransaction explicitly refused before mutation, never adopted/disposed. This is EF-owned transaction detection, not a general certification of arbitrary ambient/distributed transactions.
- Rollback uses None; secondary rollback/dispose/recovery-read failure cannot replace primary save/cancellation in the tested cleanup paths. Exact attempted/old line references are captured before detach fixups.
- Unconfirmed rollback leaves attempt detached and skips recovery; no fabricated restored graph. Confirmed rollback recovery is after disposal. No global sweep/caller dirty flush.
- New cancellation fixture cancels in second SaveChanges interceptor, after first stage; actual caller token and no pending graph are asserted. This is an EF write-boundary cancellation, not mid-wire/OS abort.
- Success disposal proved with EF DiagnosticSource TransactionDisposed, not CurrentTransaction null. Probe listens to EF diagnostics globally; fixture isolation/controlled operation window bounds its counts, not a general production resource certificate.
- Scripted rollback fault is InvalidOperationException injected before provider rollback, not an actual network outage nor proof of the separate generic provider-exception branch. Source handles both; subsequent dispose implicitly rolls back in this fixture. Recovery failure fixture preserves the original EF exception instance.

Together with accepted R1 per-group obligations/fresh detached preflight/explicit CAS/coupon refusal and accepted231 compact value/storage,232 draft-selection capability is ACCEPTED WITHIN THIS REPORTED SCOPE.

## Limits remain
No executed HTTP/live selection, no frontend save connection, no shared migration apply/runtime refresh. Sequential CAS fixtures are not simultaneous race proof. SelectionEnabled defaultfalse. Final Commit/PaymentDraft remain not_ready. Service-area destination unsupported; coupon nonblank explicitly refused pending future common read/write/final intent. EF snapshot gap retained; no scaffolding claim. General connection-loss/commit-ack ambiguity and ambient TransactionScope behavior are not certified by these tests.

Elapsed~62min vs20min NONCOMPLIANT; do not grade process PASS or repeat broad tests to compensate. One final Order integration task233 issued, actual existing process path with final goods quote + selected evidence validation before reservation and compact persisted Order facts. No user Visual ACCEPT/fullcheckout/activation claim; user review window/runtime untouched. No historical repricing/full JSON per order.
