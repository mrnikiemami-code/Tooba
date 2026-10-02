# anti-enumeration — TB-TMAR-HOST-MULTITENANCY-AMC-001-W1

Unknown host / Disabled / Suspended / non-Active → same `FailClosed()` → 404 `platform.resolution.failed`.

No status/existence detail, connection reference, or connection string in response.

SkipPrefixes preserved exact:

- `/health`
- `/ready`
- `/__platform-error`
- `/__platform-conflict`

`StartsWithSegments` unchanged. No prefix add/remove.
