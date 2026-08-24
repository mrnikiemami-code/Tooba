# TB-P04-T010 — Live payment flow

Buyer: guest cart (`sessionStorage` cartId + guest secret; secret redacted below).

## Success sample

| Field | Value |
| --- | --- |
| CheckoutGroupId | `01a035ce-401f-7000-b62c-1f3d3cae9d7f` |
| Order reference | `TB-20260824220504-01-1d92bc` |
| SellerOrderId | `01a035ce-4021-7000-8e27-c05fd2df0597` |
| PaymentId | `d46138d7-24d1-4d47-8e5f-37dd0697fa7c` |
| AttemptId | `75ee578b-d7ee-4a8e-82e7-4885f296f198` |
| Expected amount | `1951100` |
| Currency | `IRR` |
| Provider | `fake` |
| Provider request reference | `fake-d46138d724d14d478e5f37dd0697fa7c` |
| Initiation | redirect `/payment/sandbox?...` |
| Callback/result | sandbox complete → Host Verify → `Succeeded` |
| Payment final | `Succeeded` |
| Order final payment state | `Paid` |
| Duplicate initiation | same PaymentId / redirect |
| Duplicate callback | second Verify stays `Succeeded` |

## Failure sample

| Field | Value |
| --- | --- |
| CheckoutGroupId | `01a035ce-4981-7000-8142-f08f7c50db84` |
| Order reference | `TB-20260824220506-01-d6ff60` |
| PaymentId | `6f813a0e-d3c8-434b-8e2d-1c3ed8db2d56` |
| Amount / Currency | `1951100` / `IRR` |
| Outcome | sandbox `failure` → Payment `Failed` |
| Order payment state | remains `PendingPayment` |

## UI evidence checkout (Pending → sandbox → Paid)

CheckoutId `01a035ce-8286-7000-87eb-f6e08f3a7495`, PaymentId `a8e70cad-afff-4511-bc70-1dfa4971496b` (screenshots 01–05).

Guest secrets are not recorded in published evidence.
