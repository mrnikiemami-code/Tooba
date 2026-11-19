# COMPLETION-LEDGER — TB-282-R2

| ID | Method | Assertion | Provider | Observed |
| --- | --- | --- | --- | --- |
| B1-recovery | mount click recovery | identical POST body/path/method | jsdom + fetch stub | pass (`story-management-282r2.mount.test.ts`) |
| B1-same-key | storeStoryIntent | changed body refused | node:test | pass |
| B1-same-tick | runDurableStoryMutation ×2 | sends===1 | jsdom fetch | pass (282-R1 mount) |
| B2-tehran | combineTehranDayAndTime | `2025-11-19T07:00:00.000Z` | node/tsx | pass |
| B2-lookups | mount admin product URL | `/stories/product-options` not seller offers | jsdom | pass |
| B3-scope | VendorShell event + story-scope | invalidate on actor/shop | source + mount harness | landed |
| B3-grid | StoryManagementScreen | no `disableLiveGrid` | ripgrep | absent |
| B4-media | MediaStoryPlatformUpload282R1 | no Empty seller occurrence; actor-scoped replay | Host tests | 6/6 |
| B4-serve | HTTP GET | health/ready/platform + FE routes 200 | live 5088/3000 | recorded |
| AMSC | StoryModuleAmsc001* + W5 | 34 routes / 32 requests | Host tests | in 47/47 |
