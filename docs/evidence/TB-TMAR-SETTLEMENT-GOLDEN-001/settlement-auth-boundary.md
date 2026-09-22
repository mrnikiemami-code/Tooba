# Settlement auth boundary

- Endpoints define:
  - ISettlementSellerAuthorizer (Seller/)
  - ISettlementAdminAuthorizer (Admin/)
- Endpoints do NOT reference Host, SellerPanelAccess, AdminPanelAccess, ControlPlaneRegistry, or DbContext.
- Host adapters (transport/security only):
  - HostSettlementSellerAuthorizer → SellerPanelAccess
  - HostSettlementAdminAuthorizer → AdminPanelAccess + Marketplace Development synthetic tenant (moved from deleted SettlementAdminAccess)
- PlatformHttpException from auth propagates to Host ToobaExceptionHandler (not caught as Settlement business Result).
- Actor/seller/admin identity passed explicitly into Commands/Queries.
