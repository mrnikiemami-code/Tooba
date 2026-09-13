# R18 focused validation

Covered by `ReservationPolicyAdminUxTests`, `ReservationCycleFoundationTests` (resolver regression), `AdminReservationCycleAuditTests` (R17 snapshot display), `reservation-policy-admin.test.ts`, `recovery-staleness.guard.test.mjs`.

1–7 resolution + clear inherit
8–11 validation
12–14 snapshot integrity
15–16 multi-line
17–19 permissions (seller mutate permission absent ⇒ deny)
20–25 UX FA/EN
26–30 R15/R16/R17 + recovery + git diff --check
