# R20-R1 anti-pattern scan

CLEAN:

- One shared countdown formatter (`lib/reservation-countdown.ts`); Admin/Cart re-export
- No hardcoded 24h lifecycle special case
- Formatter is presentation-only; remaining seconds still from server ExpiresAt
- No per-second API fetch (`pendingFetches=1`)
- No raw enum fallback in the normal renderer
- No Status.ToString() business parse
- No broad i18n rewrite
- No CSS overflow hide

Notes: screenshot tool can still mirror RTL pixels; live `dir`/a11y tree are correct.
