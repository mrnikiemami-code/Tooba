# 01 — Shopeiva feedback source

## Source
- Component: `reference/shopeiva/src/components/dashboard/notifications/notifications.jsx`
- Route: `src/app/user-panel/notifications/page.jsx` → `http://127.0.0.1:3001/user-panel/notifications` (HTTP 200)
- Vendor-specific notification route: **not present** (`/vendor-panel/notifications` → 404). Seller Tooba reuses the same Customer Shopeiva notifications component pattern (documented in T023).

## Feedback mechanism
Source imports `toast` from `react-toastify` and calls:
- `toast.success('به عنوان خوانده شد علامت‌گذاری شد')` on mark-one-read
- `toast.info('اطلاعیه حذف شد')` on delete
- `toast.info('همه اطلاعیه‌ها قبلاً خوانده شده‌اند')` / `toast.success('همه اطلاعیه‌ها خوانده شدند')` on mark-all

## ToastContainer (providers.jsx)
```
position="top-right"
rtl={true}
autoClose={3000}
hideProgressBar={false}
newestOnTop
closeOnClick
pauseOnFocusLoss
draggable
pauseOnHover
theme="colored"
```

## Package
`react-toastify`: `^11.1.0` in Shopeiva package.json
