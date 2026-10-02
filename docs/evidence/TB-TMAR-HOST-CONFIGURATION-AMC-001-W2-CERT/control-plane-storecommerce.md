# control-plane-storecommerce — TB-TMAR-HOST-CONFIGURATION-AMC-001-W2-CERT

ControlPlaneRegistry = process-local startup snapshot (not durable CP DB). BuildRegistry pure/deterministic/zero I/O/zero service locator. IReadOnly surfaces; no live mutation path after singleton build → startup-frozen certified.
StoreCommerce: Marketplace deployment-level; SingleStore per Active tenant; Market may inherit DefaultMarketReference; DefaultCurrency/SalesChannel explicit; SalesChannel via Offer.Contracts enum.
