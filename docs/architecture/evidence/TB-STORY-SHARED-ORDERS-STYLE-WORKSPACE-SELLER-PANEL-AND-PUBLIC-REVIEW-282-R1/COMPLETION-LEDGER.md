# COMPLETION-LEDGER — TB-282-R1

| Req | Method | Assertion | Provider | Observed |
| --- | --- | --- | --- | --- |
| Gate0 | DetachAttemptOwned(ownedStoryIds) | loser never persists unrelated Modified | StoryMarketplace282Gate0Tests | 4/4 pass; PG skip disclosed |
| F1 | runDurableStoryMutation + pending UI | overwrite refuse; remount recovery; double-click one send | story-executor + mount | pass |
| F2 | NAME picker + routes + actionCodes + preview | no Guid shop input; deliberate publish confirm | StoryManagementScreen | source + mount |
| F3 | toHostGridQuery + scoped views | seller query forwards grid; no admin ui-preferences | story-api + story-saved-views | source |
| F4 | UploadForPlatformStoryAsync | replay same asset; conflict; after-save; ambient refuse; distinct keys | MediaStoryPlatformUpload282R1Tests | 5/5 |
| F5 | story-management-282r1.mount.test.ts | real jsdom mount asserts controls/storage | tsx harness | 5/5 |
| Public CTA | EnrichPublicAsync CtaHref | product Guid → Catalog slug path | CatalogStoryProductTargetAdapter | source |
