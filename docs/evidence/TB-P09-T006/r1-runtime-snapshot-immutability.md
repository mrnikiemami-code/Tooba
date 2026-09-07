# R1 Runtime — Snapshot Immutability

- Baseline order B `01a07bca-c495-7000-abb2-9c25da58681f` line window=14 / label `14 روز پس از تحویل`
- After order creation, Offer changed to NonReturnable
- Reloaded same OrderLine: window still **14**, label unchanged, `isReturnable=true`
- OrderLine snapshot was not edited directly
