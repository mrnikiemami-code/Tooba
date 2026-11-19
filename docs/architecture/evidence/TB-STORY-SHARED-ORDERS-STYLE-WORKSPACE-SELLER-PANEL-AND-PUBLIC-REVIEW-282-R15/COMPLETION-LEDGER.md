# TB-282-R15 COMPLETION LEDGER

| Gate | Method | Observed |
| --- | --- | --- |
| Pre-fix fail | mounted rejection | Dialog missing / test ✖ |
| Catch rejected fetch | sendJsonRequest + upload | transportRejectionResult |
| Unknown mapping | isUncertainFailure | status0 + transport.rejected |
| Result Dialog | real grid 2+1 | success/unknown/skipped |
| Exact retry | recovery UI after close | same body; A not repeated |
| Epoch+reject | helper batch | not_started; zero later POST |
| Zero backend | — | FE-only |
