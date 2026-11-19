# COMPLETION-LEDGER — TB-282-R3

| ID | Method | Assertion | Provider | Observed |
| --- | --- | --- | --- | --- |
| S1-empty200 | verifyStorySuccessPayload + mount | empty/`{}`/non-JSON keep envelope byte-identical; no success/nav | jsdom + node:test | pass (`story-management-282r3.mount.test.ts`) |
| S1-negative | pre-fix finalize control | empty 200 would clear without verify; verify rejects | node:test | pass |
| S1-route | bindStoryEnvelopeRoute | wrong route/method → zero fetch | node:test | pass |
| S1-storage | list/blocked | enumeration throw → blocked row, not empty success | node:test | pass |
| S1-replay | mount valid replay | same-command valid response clears matching envelope | jsdom | pass |
| S2-search | product-options request | search+page query + totalCount | jsdom fetch stub | pass |
| S2-adapter | StoryProductOptionsAdapter | ICatalogPublishedProductSearchLookup; no sampler on Story path | source | landed |
| AMSC | StoryModuleAmsc001* + W5 | 34 routes / 32 requests | Host tests | 41/41 |
| Serve | HTTP GET | health/ready/platform + FE routes 200 | live 5088/3000 | recorded |
