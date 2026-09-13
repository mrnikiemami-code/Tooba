# TB-P10-T004-R22-R1 — OTP Login

Flow: mobile → `IdentityOtpLoginService.RequestLoginAsync` → OTP → `CompleteLoginAsync` verifies challenge, finds/creates Phone User, `EstablishSessionForUserAsync`.

Development fixture: `09111111111` / `123456` only when `IsDevelopment()` or `Testing` (`IdentityModule`). Production appsettings does not list the fixture. Wrong OTP 000000 → 401.

Host tests: `Development_otp_login_fixture_issues_session_without_password` PASS.
Runtime: request+complete 200, session user `01a0996c-b8d9-7000-9854-80b3f59e8d7c`.
Browser FE BFF cookies established; redirected to shipping.
