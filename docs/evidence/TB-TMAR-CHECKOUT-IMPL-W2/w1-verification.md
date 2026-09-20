# W1 verification
- checkout_processes + CheckoutProcessTracker intact
- CheckoutProcessManager now owns orchestration (replaces CheckoutSubmitExecutor)
- TransactionScope retained in Process Manager
- No prior Process Manager runtime; no new App→App after W1 beyond baseline shrink
