# 06 — CSS / JS / Motion parity matrix (TB-P06-T010-R1)

| Component | CSS parity | Interaction | Motion | Responsive | Result |
| --- | --- | --- | --- | --- | --- |
| Customer shipping block | `bg-gray-50 rounded-xl p-4 space-y-2 text-sm` | static read | n/a | stacked rows | REPAIRED |
| Shipment list rows | `rounded-xl border p-3 bg-gray-50` | hover highlight | `transition-colors hover:bg-gray-100` | `sm:grid-cols-2` dl | REPAIRED |
| Status badges | `rounded-full px-3 py-1 text-[10px] font-medium` | n/a | n/a | wrap | REPAIRED |
| Seller action buttons | `rounded-xl px-4 py-2 text-sm font-medium` | disabled while busy | `transition-colors hover:opacity-90` | flex-wrap | REPAIRED |
| Seller stat tiles | `rounded-xl bg-secondary/60 px-3 py-3 text-center` | n/a | n/a | `sm:grid-cols-2 lg:grid-cols-4` | REPAIRED |
| Admin grid | design-system DataGrid tokens | row hover via grid | existing grid motion | horizontal scroll | JUSTIFIED (T024 baseline) |
| Inputs (tracking/carrier) | `rounded-xl border focus:ring-2 focus:ring-primary/30` | focus ring | transition-colors on focus | full-width mobile | REPAIRED |

No unauthorized spacing/typography redesign.
