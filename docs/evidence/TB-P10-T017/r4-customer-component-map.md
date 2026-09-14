# Customer panel component map — TB-P10-T017-R4

`CustomerPanelShell` is the shared architecture (`data-customer-panel-canvas`). Same Store Appearance projection; no per-route fetch.

| Chrome | Role |
| --- | --- |
| Outer | PageBackground |
| Header | HeaderSurface |
| Sidebar | Elevated |
| Main | SectionSurface |
| Drawer | Overlay |
| Stat/order/list/detail/form/empty cards | derived Card |
| Inputs | Input (`bg-surface` / remapped gray-50) |
| Status badges | Status |
| Ticket form | Card + Primary CTA |

Live nav destinations remain covered by the R3 inventory crawler.
