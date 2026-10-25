# Architect review — TB221
Source443c75817dca4d84065be73c4089fd29e9a96eba; base87a138a224a3f12b3f1e4b481a9e4cc3980f7fd5.

## Accept for ADMIN USER TRIAL; no222 task
Independent exact-diff inspection covered primary page routing/client composition, feature-owned list transport/model/UI, publication wire/model/panel and screen extraction, additive list translation projection, and absent-clear consistency fix.
Inspected committed real receipts and VIEWED screenshots01-list-primary-1440,07-editor-published-390 and08-editor-published-top-1440.
No root build/test/local runtime execution: worker-executed14 focused tests, clean typecheck and architecture guards are the evidence. Four unrelated size failures remain disclosed; no baseline widening.

Primary page now presents actual owner drafts and server draftId routes. The post/NULL draft is not mapped onto post:standard. Names come from defaultContentLanguageId registry and stored translations, two list/context reads and no per-row GET. Existing AppDataGrid+saved-view adapter reused, app supplied through public feature boundary. Legacy catalog is secondary ?legacy=1; creation is still there, not implemented by the new primary list.
Publication sends only revision tokens, uses real string enums/omitted nullable fields, synchronous write gate and explicit stale/unknown recovery. No form reseed by publication. In EDIT controls are disabled; same current selected source is disabled. This is intent, not customer activation.
Backend list field is additive; ListAsync includes translations, mapping retains actual text. Withdraw absent now reports Absent0 rather than contradictory Cleared0.

## Real execution and shared changes
Worker refreshed local Host/FE stack and applied EXACT214+219 checked-in migrations to existing nonproduction tooba_alpha/store-alpha under standing user authorization. Receipts show Shipping current20261025130021_SelectedCapturedRevision pending0, health200, Host5088 PID26600/FE3000 PID28288 at receipt time. Isolated current executable path not provided, must be reverified rather than guessing old207 artifact.
Real mounted list shows پیشتاز/post/rev2/one area. View routes directly to actual draft. Actual publish/withdraw/republish and explicit reads show selected1, cleared2, then selected3 with SAME immutable snapshot01a17389-c201-7000-8512-c2950a6e913a/source revision2. Draft fields/revision/UpdatedAt remain unchanged; no Apply was executed.
Worker left final selected3 instead of restoring initial no-selection intent; record this actual deviation, not 'no writes'. It has no customer price effect because there is still no consumer. Real startup also consumed pending catalog/pricing transport messages.

## Truthful limits
This is a visible trial, not blanket certification. Ordinary editor Apply, full picker/money matrix, publication error branches and mounted multi-draft lifecycle were NOT re-proven by the happy-path receipts; model tests/source are not browser coverage.
Screenshots have narrow/desktop content inside a larger PNG canvas; they support visual inspection of rendered content, not the claim of native390/1440 image dimensions. DOM measurements reported390 no horizontal page overflow. UI visual acceptance belongs to user. Snapshot GUID shown in normal panel is implementation detail; consider hiding in diagnostics on feedback, not another pre-trial loop.
Original received task is persisted in TASK.md by Architect because worker diff omitted it. Elapsed221 is not reported, so no timing-compliance acceptance.
Storefront, legacy price/activation path, whole-method creation and real fulfillment owner/type/membership/measurements remain incomplete. Do not say all legacy service management is replaced or publication affects customers.
Call user with list URL now; WAIT FOR THEIR TRIAL FEEDBACK before another implementation envelope.
