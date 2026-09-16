# Performance Result — TB-P10-T022-R9-R1

## Before (baseline)

| Scenario | Time |
|----------|------|
| Warm Landing under healthy FE | ~2310–2750 ms |
| Landing under FE/Host home saturation | **~90–180 s** |
| Host page resolve | ~16–20 ms |
| Host `/home` / products listing | ~2200 ms each (or hang under load) |

## After

| Scenario | Time |
|----------|------|
| Cold Landing (incl. Next compile) | **5485 ms** |
| Warm Landing ×3 | **251 / 325 / 248 ms** |
| After revalidate then warm | 676 → **~200 ms** |
| Home `/` | **1949 ms** |
| Host page resolve warm (embedded shell) | **42 ms** |

## Acceptance

- 90–120s application SSR stall **eliminated**
- Warm Landing SSR stable in **sub-second / low-single-digit** seconds
- Cold compile overhead documented separately (4.1s compile + SSR)
