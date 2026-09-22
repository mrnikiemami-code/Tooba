# antipattern-scan

Target metrics after repair:

| Antipattern | Count |
|---|---|
| localized prose matching in CartExceptionMapper | 0 |
| Contains-based exception classification in CartExceptionMapper | 0 |
| unknown InvalidOperationException swallowed to cart.rejected | 0 |

## Guard coverage

`CartArchitectureGuardTests.Cart_exception_mapper_is_stable_codes_only_without_prose_heuristics` fails if mapper contains:

- `.Contains(`
- Persian Unicode prose literals listed in parent defect
- `"Offer"` / `"Held"` prose matching
- `default:` → `CartErrorCodes.Rejected`
- Application Errors/Commands/Queries message `.Contains(` heuristics

## Focused behavior tests

- exact stable codes map
- Persian/prose messages do NOT map
- unknown IOE propagates through `TryAsync`
