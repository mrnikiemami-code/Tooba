# R10 owner development environment authorization


## Owner standing override — 2026-11-26 (Tehran)
The owner explicitly identifies existing development Host 5088, Next 3000 and PostgreSQL tooba_alpha as the reusable test environment, containing no important data. Use these for ordinary and destructive development tests, including necessary module-owned migrations, fixture creation/modification/deletion and cleanup. No new Host/database/Testcontainers provisioning and no repeated owner approval for these authorized test operations. Verify actual dev identity and candidate build before mutation; never target production or other databases. Preserve architecture, permissions and business-policy authority. This supersedes earlier isolation-only or migration-approval restrictions for this identified test environment, including TB-296-R10; it does not authorize unrelated production changes. Keep necessary recovery evidence proportionate; do not halt merely because a migration or test deletion is needed.

Architect verdict: REPAIR_REQUIRED. Resume original immutable R10 scope; absent authorized contracts are implementation work. Complete all 14 requirements, 42 cases and 8 AQ gates; continue independent work if genuine policy ambiguity remains. No TB-297. STOP_FOR_ARCHITECT.
