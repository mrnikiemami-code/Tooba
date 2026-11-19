# TB-282-R14 COMPLETION LEDGER

| Gate | Method | Observed |
| --- | --- | --- |
| Freeze epoch/scope | executeAdminOpBatch | frozen before first; re-read live before later |
| Stop remaining | mid-batch epoch bump | not_started; no second POST |
| Uncertain outcome | unverified 2xx | unknown + retained envelope |
| Per-row results UI | story-admin-op-result Dialog | title/op/outcome/reason |
| No early navigate | closeAdminOpResult | delete leave after close |
| Real grid select | AppDataGrid checkboxes | 2 eligible + 1 skipped dispatched correctly |
| Reject reason/cancel | bulk reject | 0 then 1 POST |
| Zero backend | git | FE-only |
