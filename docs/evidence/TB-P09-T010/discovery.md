# Discovery

- Detail `عملیات سفارش` used `scope="detail"` and showed all projected actions, repeating `شروع پردازش` / `بسته‌بندی` already owned by اقلام و ارسال. Grid already filtered `scope="whole-order"`.
- Domain `MarkPacked`/`PackSelections` accepted `ReadyToFulfill`, so pack could skip StartProcessing.
- Host `ResolvePackSelections` treated null/empty selections as pack-all remaining, so «بسته‌بندی انتخاب‌شده‌ها» packed siblings.
- Row kebab in `admin-order-items-shipping-panel.tsx` was hardcoded `disabled`.
- Line operational status used seller fulfillment status, so packing one line painted every row Packed.
- Mixed-status selection had no intersection rule.
