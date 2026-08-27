# 02 — Shopeiva support source map

Task: TB-P06-T025

## Source root

`D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva`

## Routes (runtime `:3001`)

| Route | Role |
|-------|------|
| `/user-panel/tickets` | Customer list |
| `/user-panel/tickets/new` | Customer form |
| `/user-panel/ticket/[id]` | Customer thread (singular) |
| `/vendor-panel/tickets` | Vendor list |
| `/vendor-panel/tickets/new` | Vendor form |
| `/vendor-panel/tickets/[id]` | Vendor thread |
| `/contact`, `/faq` | Static help (not ticket module) |
| Admin tickets | **absent** in Shopeiva tree |

## Components

| Audience | Files |
|----------|-------|
| Customer list | `src/components/dashboard/ticketsList/ticketsList.jsx` |
| Customer form | `src/components/dashboard/ticketForm/ticketForm.jsx` |
| Customer thread | `src/components/dashboard/ticketDetail/ticketDetail.jsx` |
| Vendor list/form/thread | `src/components/vendor/panel/tickets/*.jsx` |

## Visual vocabulary (locked)

- Accent `#E53935`, `rounded-2xl` cards, status chips `rounded-full`
- Thread bubbles: user red / admin gray, `max-w-[80%]`, `rounded-br-none` / `rounded-bl-none`
- Mobile: `flex-wrap`, `p-4 md:p-6`, responsive hide columns

Shopeiva ticket UIs are **demo/static**; Tooba must bind live Host APIs while preserving geometry.
