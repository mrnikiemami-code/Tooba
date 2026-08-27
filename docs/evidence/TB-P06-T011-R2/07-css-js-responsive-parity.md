# 07 — CSS / JS / responsive parity matrix (TB-P06-T011-R2)

| Surface | Property | Shopeiva source | Tooba | Verdict |
| --- | --- | --- | --- | --- |
| Customer modal overlay | display/position/z | `fixed inset-0 z-[9999]` | same | MATCH |
| Customer modal card | radius/shadow | `rounded-2xl shadow-2xl` | same | MATCH |
| Customer modal header | sticky + border-b | yes | yes | MATCH |
| Eligibility banner | amber box | `bg-amber-50 border-amber-200` | same | MATCH |
| Reason select | padding/radius | `rounded-xl px-4 py-2.5` | same | MATCH |
| Seller modal grid | 2-col date | `grid grid-cols-2 gap-3` | same | MATCH |
| Approve button | color/hover | `bg-emerald-500 hover:bg-emerald-600` | same | MATCH |
| Reject button | color/hover | `bg-red-500 hover:bg-red-600` | same | MATCH |
| Accent icon tile | brand color | `#E53935` | `#2563EB` | **JUSTIFIED** (P05/P06 contract) |
| Mobile modal | max-h/scroll | `max-h-[90vh] overflow-y-auto` | same | MATCH |
| Seller list mobile | stacking | Shopeiva list | Tooba DataGrid scroll `390×844` captures | MATCH shell |

## Interaction

| State | Shopeiva | Tooba |
| --- | --- | --- |
| hover | `transition-colors` on buttons | same |
| focus ring | focus ring on inputs | `focus:ring-[#2563EB]` |
| disabled submit | `isSubmitting` | `submitting` state |
| modal close | X button hover bg | same |

## Motion

| Property | Shopeiva | Tooba |
| --- | --- | --- |
| transition | `transition-colors` | same |
| modal backdrop | `backdrop-blur-sm` | same |
| success step | step state swap | same |

## Responsive

| Breakpoint | Evidence |
| --- | --- |
| Desktop 1440×900 | captures `01-10` |
| Mobile 390×844 | captures `11-15` |

**Unresolved material deviation:** none (accent color explicitly justified).
