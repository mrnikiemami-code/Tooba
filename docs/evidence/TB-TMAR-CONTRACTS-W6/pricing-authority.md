# Pricing authority — TB-TMAR-CONTRACTS-W6

Canonical AuthoredPrice remains owned by Pricing module.
Cart resolves quotes only via `IPriceLookupGateway` / `ICampaignCartPriceAuthority` Contracts ports.
No client price authority; no duplicated calculation in Cart; campaign-aware path preserved.
