# Settlement HTTP ownership audit

- Prior: Host/Tooba.Host/Settlement/SettlementEndpoints.cs owned all seller+admin routes.
- Now: Tooba.Settlement.Endpoints (Seller/ + Admin/) owns exact paths/verbs.
- Host Program.cs only: `app.MapSettlementEndpoints();` + authorizer DI registration.
- Host/Settlement/ folder: REMOVED.
- Settlement-HTTP-Ownership: MODULE_ENDPOINTS
- Settlement-Endpoints-State: REAL_PROJECT_PRESENT
