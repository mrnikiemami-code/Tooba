# Failure behavior
- Cart convert fail after inventory+order write inside TX → rollback (no Complete)
- Cart already converted / race → winner resolution path
- Process-state write failure → exception, no Complete
- Adapter failure → same as convert failure
- Transient exception → bubble; reservations released best-effort outside TX semantics when catch fires
- No async compensation executed
