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
1de47a4b3c82f402eb7f04d61d72f68d6bb3a6fb
\\n