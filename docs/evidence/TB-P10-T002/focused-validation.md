# Focused validation

## Backend
`dotnet test ... --filter FullyQualifiedName~StorefrontShippingCalculatorTests`
Passed=6 Failed=0

## Frontend
`node --experimental-strip-types --test app/storefront/storefront-shipping-api.test.ts`
Passed=4 Failed=0

## Recovery
`node docs/ai/recovery-staleness.guard.test.mjs` (updated for T002)
