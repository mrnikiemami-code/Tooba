# TB-P09-T022-R1 — focused validation

## FE 500 root cause

Documented in `r1-fe500-discovery.md` / `r1-fe500-fix.md`: process unavailable / cold-restart `ECONNRESET`, not Consolidated Package SSR/DTO. After FE Ready → Admin + customer routes HTTP 200.

## Host / FE regressions (T022 baseline)

- Existing T022 Host tests remain green (no domain redesign in R1).
- Existing T022 FE unit tests remain green for Consolidated Package surfaces.

## FE HTTP smoke regression

Script: `docs/evidence/TB-P09-T022/_r1_http_smoke.mjs`

Curls live FE Admin (single + multi) and customer-panel Order Detail; **exits 1** if any response is not HTTP 200.

```text
node docs/evidence/TB-P09-T022/_r1_http_smoke.mjs
```

Orders covered:

- Admin single: `01a08973-d831-7000-ae48-d6f8a6bc3fcf`
- Admin multi delivered: `01a08973-dd8c-7000-b205-cc7f15358dfb`
- Admin fresh multi: `01a0898a-0d7d-7000-b5ed-7f5d85a29241`
- Customer panel: `01a0898a-0d7d-7000-b5ed-7f5d85a29241`

Requires FE Ready on `http://127.0.0.1:3000`.

## Recovery guard

```text
node docs/ai/recovery-staleness.guard.test.mjs
```

Expect **3/3** with `CURRENT_TASK_ID=TB-P09-T022-R1`.

## Scope

- No unrelated full-repo suite
- No TB-P09-T023
- `USER_VISUAL_ACCEPTED=NO`
