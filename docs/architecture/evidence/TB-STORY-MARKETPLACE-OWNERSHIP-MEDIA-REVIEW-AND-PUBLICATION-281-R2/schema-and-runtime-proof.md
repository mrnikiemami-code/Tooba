# Schema and runtime proof (TB-281-R2)

## Schema

- Parent 281 Story migration and R1 Media IntentHash migration remain immutable and already applied.
- R2 added ZERO new migrations/DDL/seed/backfill/reset.
- Schema-State: UNCHANGED_FROM_R1_APPLIED

## Host no-seed opt-out

`Program.cs` guards the Development bootstrap block with:

`TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1`

Default behaviour unchanged when unset. Opt-out is process-invocation only; appsettings/auth/tenant/edition unchanged.

## Served refresh

Recorded at result time in RECEIPTS.md / RESULT.md after publish attempt. No Story/Media business POST authorized.
