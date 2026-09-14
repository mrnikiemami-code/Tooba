# Customer panel coverage — TB-P10-T017-R3

`CustomerPanelShell` is the shared architecture. No per-route appearance fetch.

| Chrome | Role |
| --- | --- |
| Outer | PageBackground (`bg-page`) |
| Header | HeaderSurface (`bg-surface`) |
| Sidebar | ElevatedSurface (`bg-surface`) |
| Main | SectionSurface (`bg-section-surface`) |
| Mobile drawer | OverlaySurface |
| Inner dashboard/orders/profile/settings/wishlist/address cards | CardSurface (`bg-surface`) |
| Capability empty shell | CardSurface |
| Support ticket form (shared UI used by customer tickets) | CardSurface |
| Dev wallet-checkout preview | PageBackground (`bg-page`, was `bg-[#F5F5F5]`) |

Live nav crawled: dashboard, orders, order detail (seeded checkoutId), wishlist, addresses, notifications, tickets, tickets/new, wallet, gift-cards, profile, settings.

Ticket detail `[id]` has no seeded ticket id in the dev customer; coverage is the same CustomerPanelShell + SupportTicketForm CardSurface as tickets/new (layout-static).
