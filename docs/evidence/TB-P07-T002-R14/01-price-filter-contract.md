# Price filter server contract (TB-P07-T002-R14)

Field: `offerAmountRange`

## Canonical value
User enters a decimal amount in display currency (تومان). Sent to Host as `GridFilterRequest.value` / `valueTo` strings (InvariantCulture).

## Product metric
For each product, compute **min** and **max** offer `Amount` across all linked prices.

## Match semantics
| Operator | Match when |
|----------|------------|
| equals | `min <= value <= max` (value within displayed range) |
| notEqual | outside range |
| greaterThan | `max > value` |
| greaterThanOrEqual | `max >= value` |
| lessThan | `min < value` |
| lessThanOrEqual | `min <= value` |
| between | ranges overlap: `min <= valueTo && max >= value` |
| blank | product has no priced offers |
| notBlank | product has at least one priced offer |

## Whitelist
`offerAmountRange` added to `AdminProductGridQueryPolicy.FilterableFields` with `NumberOperators`.
