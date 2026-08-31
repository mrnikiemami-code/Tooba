# Focused validation (TB-P07-T039)

## Backend
```
dotnet test --filter "FullyQualifiedName~VariantAxisCapabilityRulesTests|FullyQualifiedName~Variant_axis_capability"
→ 9 passed, 0 failed (alternate output path; live Host :5088 kept running)
```

## Frontend
```
npx tsc --noEmit → OK
node --test app/admin/variant-axis-capability.test.ts app/admin/category-attributes-panel.test.ts → 12/12 pass
```

## Guard
```
node docs/ai/recovery-staleness.guard.test.mjs → 3/3 pass
```
