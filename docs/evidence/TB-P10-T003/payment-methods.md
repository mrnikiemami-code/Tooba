# Payment methods
GET /v1/storefront/payment-methods returns only operational Store methods:
- gateway when Mode=Sandbox or configured Webhook
- manual when ManualCardToCardEnabled
- wallet via wallet-quote eligibility only (not faked in catalog)
Zero methods → unavailable UI; CTA disabled.
