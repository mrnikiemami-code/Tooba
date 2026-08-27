# 09 — FE Shopeiva lock

Task: TB-P06-T025

## Shared modules

- `src/frontend/app/support/support-api.ts` — Host/BFF client
- `src/frontend/app/support/support-ui.tsx` — list / form / thread

## Routes

| Audience | Paths |
|----------|-------|
| Customer | `/customer-panel/tickets`, `/new`, `/[id]` |
| Vendor | `/vendor-panel/tickets`, `/new`, `/[id]` |
| Admin | `/admin/tickets`, `/[id]` |

## Locked visuals

- Accent `#E53935`, `rounded-2xl` cards, status chips `rounded-full`
- Thread: self bubble red `rounded-br-none`; support gray `rounded-bl-none`; `max-w-[80%]`
- No attachment upload UI
- No chatbot / SLA / unread badges

## Call pattern

- Customer mutations/reads: `/api/customer/support/...`
- Seller/Admin: `/v1/seller|admin/support/...` via Next rewrite
