# Accepted Lineage — Checkout W4 → HEAD

Chronological Worker-completed / committed lineage (Architect ACCEPT may lag Worker PASS; durable SoT notes called out).

| Task-ID | Parent (task narrative) | Commit (feat/docs) | What changed | Resulting state | Next in Result/SoT |
| --- | --- | --- | --- | --- | --- |
| TB-TMAR-CHECKOUT-IMPL-W4 | W3 | `b4fb7702` / `0440354a` | Order.Infra inventory Cancel/Restore/PaymentBridge → Inventory.Contracts | W5 READY; TX preserved | W5 |
| TB-TMAR-HOST-STRUCTURE-W1 (+R1) | — | `85a1823a` / `9c83fe01` / `bda3f977` / `f58744c5` | Host folders + durable locks / FE freeze | Checkout resume READY | W4 then W5 |
| TB-TMAR-CHECKOUT-IMPL-W5 | W4 | `d73d2aae` / `af08138a` | ICheckoutPromotionPort; Order.App↛Promotion.Application | **PAUSED_AT_SAFE_W5_CHECKPOINT**; W6 READY but deferred | Offer track / W6 deferred |
| TB-TMAR-OFFER-REFERENCE-W1 | Checkout pause | `a9e6ee4f` / `5f58ce2d` (+ tip-aligns) | Offer Endpoints+Tests; MapOfferModule; COMPLETE_REFERENCE_PATTERN | Offer golden pattern claimed | Tax/Pricing candidates |
| TB-TMAR-TAX-REFERENCE-W1 | Offer W1 | `edecccce` / `400bed8c` | Tax Endpoints+Tests; MapTaxModule | PROVEN_ON_2_MODULES (historical) | Pricing |
| TB-TMAR-PRICING-REFERENCE-W1 | Tax W1 | `ccc90672` / `2ed35399` | Pricing Endpoints+Tests; MapPricingModule | PROVEN_ON_3_MODULES (historical) | next module / Checkout pause |
| TB-TMAR-OFFER-REFERENCE-W1-R1 | Offer W1 reopen | `17597ddb` / `7127e01c` | Physical folders/namespaces; ARCH-MODULE-PHYSICAL-001; VERIFIED_ON_DISK | COMPLETE_REFERENCE_PATTERN revalidated | Architect ACCEPT / Pricing gated wording |
| Git hygiene chores | — | `da2821b4`…`9c2251a0` | .gitignore + evidence triage | clean tree | — |
| TB-TMAR-OFFER-REF-W1 | stale discovery | `6eb436d7` / `04342c04` | RECOVERY_CONFLICT evidence only | BLOCK stale task | Resync |

Architect acceptance recording: Worker PASS artifacts + recovery-sot.md exist under each `docs/evidence/TB-*`. Master/Bootstrap still phrase Offer R1 as awaiting Architect ACCEPT (see Durable-State-Consistency).
