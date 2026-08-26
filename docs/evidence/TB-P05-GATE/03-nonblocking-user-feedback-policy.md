# 03 — Non-blocking user feedback policy (TB-P05-GATE)

## Locked policy

| Rule | Status |
|---|---|
| Pipeline never waits for manual user visual review | **LOCKED** |
| Home/PDP feedback may arrive asynchronously | **LOCKED** |
| Later user complaint → focused Repair Task | **LOCKED** |
| Pending manual visual review **≠** Pipeline BLOCK | **LOCKED** |
| Functional PASS **≠** Visual ACCEPT | **LOCKED** |
| No immediate user confirmation required to issue next Task | **LOCKED** |

## SoT fields (this gate)

```text
HOME_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
PDP_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
```

Worker must **NOT** write `FINAL_USER_ACCEPTED` for Home/PDP.

## Future feedback action

User opens side-by-side URLs (Shopeiva `:3017` vs Tooba `:3000`) and reports specific deltas → Architect issues targeted Repair (e.g. Home section X hover shadow). Pipeline continues without blocking on absence of feedback.

**Policy lock: COMPLETE**
