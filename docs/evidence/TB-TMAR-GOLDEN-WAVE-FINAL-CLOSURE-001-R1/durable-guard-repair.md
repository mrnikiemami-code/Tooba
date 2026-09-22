# Durable guard repair

`TmarDurableGuardTests` now enforces:

- one active authoritative current-state heading in MASTER and BOOTSTRAP;
- no current `Remaining:` or `NEEDS_APPLICABILITY_REVERIFY` state;
- no Offer entry in an active internal-only module section;
- JSON agreement for Golden Wave completion, next task, and gate;
- empty reopened/applicability-review arrays after completion;
- exactly 11 complete modules and exactly 10 HTTP manifest modules;
- Inventory excluded from the HTTP manifest and classified as
  `INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES`;
- Inventory's canonical full accepted commit SHA.

Historical task chronology remains allowed after the explicit
`HISTORICAL / SUPERSEDED` boundary.
