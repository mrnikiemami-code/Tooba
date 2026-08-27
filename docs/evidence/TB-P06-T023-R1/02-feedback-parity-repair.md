# 02 — Feedback parity repair

## Change
- Removed invented inline-flash banner from `notification-inbox.tsx`
- Restored `react-toastify` `toast.success` / `toast.info` / `toast.error` with **exact Shopeiva copy** for mark-read / delete / mark-all
- Mounted `ToastContainer` in `app/providers.tsx` with the same props as Shopeiva `providers.jsx`
- Added dependency `react-toastify@^11.1.0` (same major as source)

## Unrelated UI
No Storefront/Admin redesign. Shared inbox component still used by Customer and Seller pages.
