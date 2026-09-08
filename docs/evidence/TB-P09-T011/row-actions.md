# Row actions

`lineLifecycleActions` prefers backend line projection; otherwise seller-level `pack_selected`/`unpack` when the line has packable/unpackable qty.

Kebab renders only when `lineActions.length > 0`. Hidden when no lifecycle action applies.

Runtime after StartProcessing on `01a07ec5-7b94-7000-8e14-9b81ae2261d1`: per-line `pack_selected` projected for all three fulfillment items. After packing L1 qty 1: L1 also projects `unpack`.
