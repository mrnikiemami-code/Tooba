# R1 Regression — TB-P09-T020-R1

Preserved T020 / T016 / T019:

- Remainder 0.75 of 1.25 packable/shippable after first 0.50 dispatch
- Whole-order cancel blocked after first dispatch (LOCK-OPS-002 / T016)
- Split-delivery return 0.25 after deliver A+B (T019)
- T016 cancelled paid checkout has no `request_return`
- Invoice / settlement / return locks not redesigned

Domain: last remainder dispatch stays `Dispatched` until deliver (R1 terminal-dispatch).
