# 02 — Frontend validation

Task: TB-P06-T026-R1

## Commands

```text
cd src/frontend
npm run typecheck
npm run lint
npm run test
npm run test:wallet
npm run build
```

## Results

| Check | Result |
|-------|--------|
| typecheck (`tsc --noEmit`) | exit **0** |
| lint (`next lint`) | **No ESLint warnings or errors** |
| `npm run test` | all suites **fail 0** |
| `npm run test:wallet` | **4** pass |
| production `build` | exit **0** (routes include customer wallet/gift-cards + admin gift-cards/wallets) |

Logs: `frontend-typecheck.log`, `frontend-lint.log`, `frontend-test.log`, `frontend-build.log`
