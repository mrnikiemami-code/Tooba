# R3 expiry worker — TB-P09-T022-R3

`ReleaseExpiredHoldsAsync` selects:

```sql
status = 'Held'
AND expires_at IS NOT NULL
AND expires_at <= utcNow
```

Committed paid holds have `ExpiresAt = null`, so they are excluded. No worker query change required beyond the commit promotion.
