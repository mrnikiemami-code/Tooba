# TB-P10-T004-R22-R1 — Security

- `sanitizeReturnTo`: internal locale paths only; rejects `https:`, `//`, secrets, `/login`
- No session fixation: new session issued on OTP complete; logout revokes
- No cross-customer merge; guest secret required
- Fixture OTP not in Production appsettings; Enabled only Dev/Testing
- OTP success/fail logged as event names, not codes
- Checkout auth is backend-enforced
- Customer identity from session UserId, not cart token
- Authenticated checkout Convert uses `CartAccess(session.UserId, guestSecret)`
