# Gateway
Dev Mode=Sandbox: fake provider + /payment/sandbox redirect (not UI success).
Prod Mode=Disabled or unconfigured Webhook: gateway omitted from catalog; spoofed initiate → payment.method.unavailable.
Configured Webhook: offered when InitiateBaseUrl + signing secret present.
