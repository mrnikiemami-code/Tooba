# TB-282-R10 COMPLETION LEDGER

| Gate | Method | Observed |
| --- | --- | --- |
| A preview | FE object URL + tests | Local preview markers; video hides duration; modal ended path retained |
| A duration | executor + screen | Image seconds→ms; video durationMs null |
| B window | Create/Update StartAt/EndAt + SetDraftPublicationWindow | Draft Status unchanged; Jalali/Tehran parse tests pass |
| B list clock | formatStoryScheduleClock | fa day+HH:mm |
| C delete | CQRS DELETE endpoints + Dialog | actionCodes delete; confirm cancel no request; seller editable only |
| C replay | TryReplayDeleteAsync | Tombstone when story missing after occurrence |
| D status | displayState only | No stacked badges; awaiting term unified |
| E submit | HasPlayableContent seller asset | Live GET contentReady false / no submit for URL-only seed |
| Proof | Host 8/8; FE 4/4; tsc | Pass |
| Serve | Host5088 skip; Next3000 | health 200; stories routes 200; next build Html/404 disclosed |
| Zero | — | No migration; no live Story/Media write; no grants |
