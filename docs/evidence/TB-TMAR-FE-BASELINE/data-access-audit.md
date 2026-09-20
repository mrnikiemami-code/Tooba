# Data access audit — TB-TMAR-FE-BASELINE

## Pattern observed

- **Server-side fetch**: storefront `*-api.ts` modules (`storefront-api`, composition, landing, home loaders) called from async Server Components / `generateMetadata`.
- **Client fetch**: admin screens, panels, cart interactions via `admin-api.ts`, panel `*-api.ts`, `host-client` helpers; CSRF for mutations (`lib/auth/csrf`).
- **Wrappers**: many capability-local `*-api.ts` files plus oversized `admin-api.ts`.
- **Caching/revalidation**: Next fetch defaults / explicit tags uneven; not standardized on one client cache library.
- **No TanStack Query / SWR** in package.json — do not assume required.

## Recommendation

**MIXED_STRATEGY_RECOMMENDED**

- Keep server loaders for SEO-critical storefront reads.
- Centralize shared HTTP/host/locale/csrf helpers (`CENTRALIZE_EXISTING_WRAPPER` for host origin + error mapping).
- Split `admin-api.ts` by capability over FE-F2 (not install React Query unless a later task proves client cache complexity).
- Do **not** install a dedicated client data library in this baseline task.

Verdict code: `MIXED_STRATEGY_RECOMMENDED`.
