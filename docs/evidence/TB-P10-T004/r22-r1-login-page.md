# TB-P10-T004-R22-R1 — Login Page

Canonical route: `src/frontend/app/login/page.tsx` inside StorefrontShell.

- `/fa/login` RTL — heading ورود به حساب, mobile then OTP
- `/en/login` LTR — Sign in, Mobile number, Send code
- No `type=password`, no modal, no social, no registration redesign
- Loading/error via `login-error`, send/verify busy labels
- Header «ورود» → `/login`
- Browser: FA login → OTP 123456 → `/fa/shipping` (returnTo)
