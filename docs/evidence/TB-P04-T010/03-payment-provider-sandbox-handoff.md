# TB-P04-T010 — sandbox / dev provider handoff

Provider code: `fake` (`FakePaymentGateway`).

Redirect shape (Host-composed, no real bank brand):

```text
/payment/sandbox?paymentId=…&attemptId=…&ref=…&checkoutId=…
```

UI banner (explicit):

```text
SANDBOX / DEV PROVIDER
این صفحه بانک واقعی نیست. نتیجه فقط پس از تأیید سرور ثبت می‌شود.
```

Visual: `sandbox-handoff-desktop.png` (same live URL after initiation).

Outcome buttons call `POST /v1/storefront/payments/{paymentId}/sandbox/complete`. Frontend does not mark Paid; Host `VerifyAsync` is authority.
