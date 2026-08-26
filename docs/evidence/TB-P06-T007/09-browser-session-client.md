# 09 — Browser session client (TB-P06-T007)

## File

`src/frontend/lib/auth/browser-session.ts`

## Exports

| Function | Purpose |
|---|---|
| `readCsrfCookie()` | Parse `tooba_csrf` from `document.cookie` |
| `bffFetchHeaders(json?)` | Build headers with `Accept`, optional `Content-Type`, `X-Tooba-Csrf` |
| `ensureCsrfCookie()` | Fetch `/api/auth/csrf` if cookie missing |

## Usage pattern

```typescript
await ensureCsrfCookie();
await fetch("/api/customer/profile", {
  method: "PUT",
  credentials: "include",
  headers: bffFetchHeaders(true),
  body: JSON.stringify(payload),
});
```

## Constants

Imported from `src/frontend/lib/auth/constants.ts`: `CSRF_COOKIE_NAME`, `CSRF_HEADER_NAME`.
