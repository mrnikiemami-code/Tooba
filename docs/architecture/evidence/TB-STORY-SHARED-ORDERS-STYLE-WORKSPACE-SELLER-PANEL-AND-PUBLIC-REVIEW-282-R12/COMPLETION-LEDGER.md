# TB-282-R12 COMPLETION LEDGER

| Gate | Method | Observed |
| --- | --- | --- |
| Header no-end | StoryPublicationWindowFields | Outside date columns in create+edit |
| Stable end column | Always mounted + disabled | Toggle does not shift start/end tops |
| Desktop align | CDP 1440 edit | dayDiff=0 timeDiff=0 |
| Mobile order | CDP 390 edit | start then end; overflowX=0 |
| A11y structure | mount + DOM | no nested label; htmlFor checkbox |
| Semantics | parseOptionalPublicationWindow | requireEnd / null end preserved |
| Serve | next-dev :3000 | create+edit forms live |
| Zero backend | — | No BE change |
