# Recovery SoT sync (TB-P07-T036-R1)

Updated existing documents (no competing system):

- `docs/ai/TOOBA-RECOVERY-CONTEXT.md`
- `docs/PROJECT-STATE.md`
- `docs/ai/pipeline-runtime-policy.json` (unchanged protocol; Worker IDLE between tasks)

## Recorded state
| Field | Value |
|--------|--------|
| Phase | P07 |
| Last Architect-accepted before this repair | TB-P07-T035 |
| Implemented under review | TB-P07-T036 |
| Current repair | TB-P07-T036-R1 |
| Branch | main |
| USER_VISUAL_ACCEPTED | NO |
| Next | Worker IDLE / waits for Bridge Task (no invented next task) |

## Staleness guard
`docs/ai/recovery-staleness.guard.test.mjs` — repo-local, no Bridge dependency.


Final HEAD/origin (TB-P07-T036-R1):

\	ext
d5598a6ef77a33d4241639d630acdef81b3a3d0d
\\n