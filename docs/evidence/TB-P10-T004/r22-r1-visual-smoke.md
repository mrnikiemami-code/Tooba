# TB-P10-T004-R22-R1 — Visual Smoke

Login uses StorefrontShell + Shopeiva chrome. No redesign of Home/PDP.

- FA `/fa/login`: RTL dir, ورود به حساب, mobile then OTP step, blue #2563EB CTA
- EN `/en/login`: LTR copy Sign in / Mobile number / Send code
- Mobile + desktop width of existing shell
- OTP progression: ارسال کد → کد یک‌بارمصرف → ادامه
- No technical error codes on the form
- USER_VISUAL_ACCEPTED=YES remains a task flag for already-accepted surfaces; new Login is smoke-only

Browser also completed fixture OTP and landed on `/fa/shipping` with storefront shipping UI.
