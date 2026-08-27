# 11 — Final validation (TB-P06-T016-R1)

## Frontend validation (required)

| Check | Expected / Result |
|---|---|
| `npm run typecheck` | **PASS** |
| `npm run lint` | **PASS** |
| `npm run test` | **PASS** |
| `npm run build` | **PASS** |

Working directory: `src/frontend`.

## Backend

| Check | Result |
|---|---|
| Backend source changed | **No** (unchanged) |
| Host health live/ready | **200** / **200** (runtime triad) |

## Scope of R1 code repair

| Item | Status |
|---|---|
| `LocaleProvider` syncs `documentElement.lang` / `dir` from public URL prefix | Applied |
| Stale `.next` cache / zod vendor-chunk 500 | Cleared + Frontend restarted |
| Unrelated UI / Story / multilingual feature work | **Not** introduced |

## Evidence completeness

| Artifact | Present |
|---|---|
| `01`–`12` markdown evidence | Yes (this package) |
| `_acceptance-proof.json` | Yes |
| `captures/01`–`10` PNGs | Yes |
| Proof script `scripts/prove-t06-t016-r1-acceptance.mjs` | Yes |

## Source-of-Truth (Worker write)

```text
P06 = IN_PROGRESS
TB-P06-T015 = ACCEPTED
TB-P06-T016 = REPAIR_REQUIRED
TB-P06-T016-R1 = AWAITING_ARCHITECT_ACCEPT
PUBLIC_LOCALE_ROUTING = PREFIXED
```

Updated files: `docs/PROJECT-STATE.md`, `docs/ROADMAP.md`, `docs/ai/TOOBA-RECOVERY-CONTEXT.md`, `docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md`.

## Predecessor

Verified: `5db5ddc220842a98e0447bafa4d885edf62397a6`

Bridge UUID: `af52fa7d-9b02-4664-9b6f-bad81f44879e`

## Commit message (required by Task)

```text
test prove locale-prefixed routing acceptance [TB-P06-T016-R1]
```

## Verdict

Validation gate for acceptance repair is **PASS** pending Architect review. Worker PASS ≠ Architect ACCEPT.
