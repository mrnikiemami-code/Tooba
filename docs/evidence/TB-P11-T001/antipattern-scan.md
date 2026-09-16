# Anti-pattern scan — TB-P11-T001

| Antipattern | Present? | Where |
| --- | --- | --- |
| Polling / magic interval | Partial | `admin-order-detail-screen.tsx` `setInterval` for reservation countdown (UI only; not fetch-poll) |
| Hardcoded first item/seller | Low residual | Landing first-section CTA; shipping seed names; no first-seller shortcut in grid paths |
| Raw GUID/enum in Admin UI | **Yes** | wallets ActorUserId; tickets guid placeholder; attributes guid placeholder; promotions seller ids; page composition sectionType keys |
| Simplified tables vs AppDataGrid | **Yes** | Menus `<table>`; Page composition list; Tickets cards |
| Duplicated grid implementations | Partial | Shared ServerGridPage/ClientGridPage good; inconsistent columns/actions; few modules mark `orders-canonical` |
| Hardcoded languages | **Yes** | Menus localeLabel fa/en; chrome fa/en; settings mixed EN |
| Suppressing errors | Partial | ACC/shell catch → caps=null fail-open; some moderate actions ignore failure UI |
| TODO/mock/placeholder production | Preview-only OK | landing preview-placeholder assets guarded; attribute GUID placeholders are UX debt |
| Storefront theme leak into Admin/Seller chrome | Guarded | isolation tests; preview exception documented |
| Marketplace/single-store leakage | **Yes** | sellers/settlement/payouts/dashboard not edition-gated; ACC/Admin auth edition hardcode |
| RabbitMQ remnants | Forbid-only | packages absent; docs forbid |
| Test-pass operational hacks | Watch | ACC fail-open “bootstrap until tuples exist” is production-dangerous |

Verdict: gaps documented for P11 roadmap; no silent suppressions introduced by T001.
