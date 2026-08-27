# 11 — Seller Story attribution (TB-P06-T019-R1)

## Storefront

No seller badge, seller name chrome, or origin label on the public Story rail/viewer.

- `home-stories.tsx` has no seller/origin attribution UI.
- Public snapshots do not drive seller-specific storefront chrome.

## Management panels only

Seller identity / origin appear in **admin** Story management (`showOrigin`, `showSellerOwner`) for review context. Seller panel hides those columns (own scope is implicit).

Attribution for review ≠ storefront branding.
