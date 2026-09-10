# R1 Focused Validation

## Backend
`dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter FullyQualifiedName~StorefrontShippingCalculatorTests -o .tmp-t002r1-test-out`

**Passed=6 Failed=0**

## Frontend
`node --experimental-strip-types --test app/storefront/storefront-shipping-api.test.ts` (cwd `src/frontend`)

**Passed=4 Failed=0**

## Recovery
`node docs/ai/recovery-staleness.guard.test.mjs` (`CURRENT_TASK_ID=TB-P10-T002-R1`)

**Passed=3 Failed=0**

## Diff hygiene
`git diff --check` — clean

## Runtime
`node docs/evidence/TB-P10-T002/_r1_runtime.mjs` — all steps `ok:true` (`r1-runtime-raw.json`)

**Focused total: 13/13 PASS** (6+4+3). No product code defects exposed; prep-day overrides + evidence only.
