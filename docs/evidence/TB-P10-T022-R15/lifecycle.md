# TB-P10-T022-R15 — Lifecycle

## States

`MerchandisingCampaignLifecycleStatus`: Draft | Published | Archived

## Runtime active (derived)

```text
IsRuntimeActive(now) =
  LifecycleStatus == Published
  && now >= StartAt
  && (EndAt is null || now < EndAt)
```

## Consequences

| State / window | Public rail active? |
| --- | --- |
| Draft | Never |
| Published + now &lt; StartAt | No (future/teasing queryable later) |
| Published + in window | Yes |
| Published + now &gt;= EndAt | No (expired naturally) |
| Archived | Never |

## Non-goals

- No per-second activation job as truth
- No stored countdown timer
- No silent global “one AMAZING campaign” uniqueness constraint
- Window validation: when EndAt set, EndAt must be strictly after StartAt
