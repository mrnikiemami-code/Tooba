# Refactor / size locks — TB-TMAR-BOUNDARY-V1-R1

Canonicalized in `docs/architecture/TMAR-architecture-locks.md`:

## ARCH-SIZE-001
No new hand-written source file may exceed 800 physical LOC without explicit architecture approval.

## ARCH-SIZE-002
Existing oversized legacy files may only stay equal or shrink; growth above baseline forbidden. Baseline entries must disappear/reduce when files are split or deleted.

## ARCH-REFACTOR-001
Before decomposing any CRITICAL_GOD_FILE:

1. Identify behaviors / use-cases
2. Add focused characterization tests around the slice being changed
3. Preserve public behavior
4. Split incrementally by capability / use-case
5. Keep architecture guards green
6. Do not rely only on AI-generated diff inspection

Do NOT create unit-test projects for all modules now — testing follows touched/refactored slices.
