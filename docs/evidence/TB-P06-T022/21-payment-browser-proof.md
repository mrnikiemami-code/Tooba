# 21 — Payment browser proof

**Task:** TB-P06-T022  
**UI lock:** absolute — no Checkout / Payment Result redesign.

## Capture status

```text
BROWSER_CAPTURE_MAY_BE_DEFERRED = YES
USER_VISUAL_ACCEPTED = NOT CLAIMED
```

When captured during worker runtime, store screenshots under this evidence folder (or `browser-proof.json` index) for:

| Shot | Route / surface |
|---|---|
| Checkout before payment | `/fa/checkout` |
| Pending / redirect (if applicable) | sandbox or result pending |
| Verified success (sandbox) | `/fa/payment/result` |
| Verified failure / safe failed case | sandbox fail path |
| Admin payment ops | Admin order detail payment Info fields |
| Mobile critical payment UX | same routes, narrow viewport |

## Proof goals

1. UI remained visually unchanged except truthful state data.
2. Success only after backend-verified success (sandbox acceptable for demo).
3. Admin shows payment fields via existing Info components — no new chrome.

## Honest note

Absence of attached PNG/WebP in this commit does **not** imply Visual ACCEPT.  
Functional payment hardening ≠ `USER_VISUAL_ACCEPTED`.
