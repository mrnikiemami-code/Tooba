# Wallet HTTP Ownership Audit

- Host/Wallet/WalletEndpoints.cs: REMOVED
- Module Endpoints: Tooba.Wallet.Endpoints present
- Host maps only MapWalletEndpoints() + AddWalletEndpointPresentation()
- Customer routes: GET /v1/customer/wallet, GET ledger, POST gift-cards/redeem
- Admin routes: gift-cards CRUD/revoke, wallets get/ledger/adjust, demo-preview
- Endpoints use ISender + ApiResponseFactory; no IWalletDirectory
