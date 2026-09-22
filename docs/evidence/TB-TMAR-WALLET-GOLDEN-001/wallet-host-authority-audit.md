# Wallet Host Authority Audit

- WalletEndpoints.cs absent
- No HTTP IWalletDirectory ownership
- No manual wallet/gift-card error JSON in Host HTTP
- Wire DTOs live in Endpoints
- WalletDbContext only in WalletDevelopmentSeedHost (dev bootstrap allowlist)
- Program registers authorizers + presentation + MapWalletEndpoints()
