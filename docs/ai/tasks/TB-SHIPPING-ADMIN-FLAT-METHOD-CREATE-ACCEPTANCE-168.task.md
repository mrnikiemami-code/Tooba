# TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-ACCEPTANCE-168 — task record (derived)

> This is a durable traceability record, **not** the original downloadable `.task.md` payload.
> The Bridge API exposes no task-content endpoint (`GET /api/tasks?includeContent=true` returns the
> row without `Content`), so the wake payload could not be re-persisted verbatim at closure time.
> The scope below is the scope that was actually executed and evidenced.

| Field | Value |
| --- | --- |
| Task-Id | `TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-ACCEPTANCE-168` |
| Bridge row id | `ec662ebc-4f24-4e04-90d7-99f1ee0485bd` |
| Channel | `tooba-main` |
| Created (UTC) | 2026-10-17T04:31:43.1138393 |
| Claimed (UTC) | 2026-10-17T04:32:04.0747795 |
| Parent | `TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-167` (PARTIAL: no live runtime proof) |
| Evidence | `docs/architecture/evidence/TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-ACCEPTANCE-168/` |

## Scope executed

Close TB-167 runtime-acceptance gap on the real flat Add Service operation, against an isolated
disposable environment only:

1. authorized Admin `POST` create (canonical method code + per-language names) on an isolated Host;
2. authoritative `GET` read-back of the created method (inactive, server-derived legacy binding,
   saved translations);
3. real browser create → grid refresh → reopen detail tabs → full-document reload persistence;
4. duplicate-code rejection and stable error code;
5. customer non-selectability of the created (inactive) method;
6. fail-closed authorization (anonymous / ungranted) with no persistence;
7. teardown of every task-owned resource, with shared `:3000` / `:5088` / `:5432` untouched.

## Constraints honored

- no change to the flat-method create contract, server-derived legacy bindings, shipping
  groups/rate authority, or any production code;
- no shared database, shared host, shared frontend, migration, feature-flag change, real SMS/PSP,
  stash/reset/rebase/force-push;
- no unrelated dirty file staged or committed.
