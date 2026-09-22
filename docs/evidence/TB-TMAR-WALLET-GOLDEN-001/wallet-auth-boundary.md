# Wallet Auth Boundary

- IWalletCustomerAuthorizer in Endpoints; HostWalletCustomerAuthorizer resolves session or X-Tooba-Dev-Actor-User-Id in Development
- IWalletAdminAuthorizer in Endpoints; HostWalletAdminAuthorizer wraps AdminPanelAccess + capability checks
- Capabilities: giftcard.view, giftcard.manage, wallet.view, wallet.adjust
- AuthorizationDecisionKind.Unavailable remains fail-open (compatibility)
- Adapters contain no IWalletDirectory / DbContext / business logic
