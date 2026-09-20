# Admin API classification — TB-TMAR-FE-F1

| Path | Class |
| --- | --- |
| `lib/admin/admin-result.ts` | SHARED_TECHNICAL |
| `admin-api.ts` re-exports of AdminResult/header | SHARED_TECHNICAL |
| Remaining `admin-api.ts` capability methods | CAPABILITY_SPECIFIC_DEBT |
| `features/admin-languages/api/language-api.ts` | capability-owned (migrated) |
| Other `*-api.ts` under flat admin | CAPABILITY_SPECIFIC_DEBT / NEEDS_REVIEW for later waves |

No TanStack Query/SWR. Auth/locale/retry semantics unchanged.
