# R3 payment provider discovery

Existing providers: fake/fake-fail (non-Prod), wallet (all envs), webhook/fail-closed (Prod), manual (R2 non-Prod only).

Config boundary: `Payment:Gateway` (`PaymentGatewayOptions`) — Mode, DefaultProvider, webhook URLs/secrets.

No store-level payment-method settings UI; enablement via configuration is the existing pattern.

R3: register `ManualPaymentGateway` in Production + Development; gate availability with `ManualCardToCardEnabled` (default false).
