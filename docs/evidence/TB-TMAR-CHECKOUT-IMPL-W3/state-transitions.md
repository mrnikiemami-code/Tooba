# State transitions (W3 Cart)
- Before convert: process MarkCartCommitting (after order persist)
- After convert success: MarkPaymentPending then scope.Complete
- Race (convert throws, cart already Converted): resolve winner, MarkPaymentPending, Complete
- Rollback: exception before Complete → ambient TX abort; optional inventory ReleaseAsync best-effort
- Duplicate submit: existing checkout → ReconcileCartConversionAsync (Infra path)
