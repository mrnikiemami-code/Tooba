# TB-P10-T022-R19 — Checkout Revalidation

CheckoutDirectory.ResolveCheckoutLineQuoteAsync:
- if MerchandisingCampaignId present → ICampaignCartPriceAuthority
- else / ineligible → Base ResolvePriceAsync
Before atomic commit, QuotedAmount/Currency/PriceId compared to fresh quote.
Mismatch → InvalidOperationException("PRICE_CHANGED"); Cart stays Active; no Payment navigation.
OrderLine snapshots accepted quote amounts + PriceId; historical totals do not query live campaign tables.
