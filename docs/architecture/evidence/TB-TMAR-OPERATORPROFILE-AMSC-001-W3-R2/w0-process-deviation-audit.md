# TB-TMAR-OPERATORPROFILE-AMSC-001-W3-R2 — W0 Process-Deviation Audit

## Norm

Analyze waves are normally **analysis-only** unless separately authorized by the
Architect. They record verdicts, blockers and evidence; they do not mutate the
repository.

## What W0 actually did

`TB-TMAR-OPERATORPROFILE-AMSC-001-W0` (commit `639d73ea`) additionally changed
repository-global **non-production** recovery/guard files:

1. `docs/architecture/tmar-module-structure-manifests.json` — merged a pre-existing
   duplicated top-level `modules` key (a Notification-only shadow array introduced by
   the Notification W3 commit `763dab13` that last-key-wins shadowed the canonical
   24-module certified array) back into a single 25-module certified array.
2. `src/backend/Host/Tooba.Host.Tests/Architecture/TmarCompleteReferenceStructureGateTests.cs` —
   reconciled the frozen expected certified-module list to include `Notification`.

## Why the deviation was necessary

The duplicate-key regression made certified truth **unreadable**: JSON last-key-wins
dropped 24 of 25 module entries, causing `OperatorProfileModuleAmcW4CertGuardTests` to
fail (`Sequence contains no matching element`) and the global structure gate to see an
incomplete certified set. Without repair, no OperatorProfile wave could validate.

## Why it is retained

Reverting the repair would restore **invalid repository truth** (a manifest that parses
to a 1-module certified set). The merged single-array manifest and the reconciled frozen
list are the correct durable state; the deviation therefore stays.

## Precedent classification

This is recorded as a **ONE-OFF PREREQUISITE RECOVERY DEVIATION**, NOT a reusable
Analyze precedent:

- It does NOT authorize future Analyze waves to repair unrelated or repository-global
  defects.
- Future such defects must be surfaced as a prerequisite blocker or handled by a
  separate Architect-authorized repair task.
- W0 history is NOT rewritten by this audit; the W0 commit message and SoT record
  already disclosed the repair explicitly.

## Scope guard for the future

Any worker encountering a global regression during an Analyze wave must either:
record it as a blocker in the Analyze result and stop, or execute only the analysis
and file the repair decision to the Architect. Silent global repair is not normalized
by the W0 exception.
