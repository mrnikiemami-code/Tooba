# R1 Runtime — Snapshot Immutability

- Baseline order A `01a07bcf-f2e2-7000-9065-22c6dc804501` line window=7 / label `7 روز پس از تحویل`
- After order creation, Offer changed to Custom 21 (and store default not required)
- Reloaded same OrderLine: window still **7**, label unchanged, `isReturnable=true`
- OrderLine snapshot was not edited directly
