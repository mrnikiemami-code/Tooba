# TB-P10-T004-R24 — Security

- Anonymous shipping/checkout 401 `checkout.authentication_required` (B, N, V)
- Merge requires guest secret + bearer; cartId alone is not ownership (R22-R1 lock)
- Logout 204 then bearer cart GET 401 (Z)
- Admin abuse PUT without actor 401 (V-admin-protected); seller panel has reservation-policy readonly
- Dev OTP `DevelopmentOtpLoginFixtureOptions` enabled only Development/Testing; not read from Production appsettings (`IdentityContracts.cs`, `IdentityModule` Production guard)
- LOCK-SF-111 safe returnTo (no open redirect) unchanged
- No guest/payment secrets in login query
- Wrong-customer cancel/hide remain ownership-checked on Host (existing R13/R22 contracts)
