# Anti-Pattern Scan — TB-P10-T022-R5

| Pattern | Status |
|---|---|
| TemplateId/IsDemo on operational Product/Category/Brand | CLEAN |
| Simplified TemplateProduct/Category/Brand | CLEAN (field-parity mirrors) |
| Invented demo lifecycle fields | CLEAN |
| In-memory Fashion SoT remaining | CLEAN (throws if called; FE loads Host) |
| Template+store union in preview | CLEAN |
| Third-party hotlinks | CLEAN (local `/images/fashion-template`) |
| N+1 preview | CLEAN |
| Non-idempotent seed | CLEAN (key guard) |
| Full clone implementation | CLEAN (not done) |
| Other-industry expansion | CLEAN (Fashion only) |
