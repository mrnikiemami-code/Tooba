# migration-plan — TB-TMAR-HOST-ADMIN-GRID-AMC-001

Recommended: **A. DELETE_DEAD_ADMIN_GRID**

Next bounded task (<=12 min):

`TB-TMAR-HOST-ADMIN-GRID-AMC-001-W1`

Scope:
1. Delete `src/backend/Host/Tooba.Host/Admin/Grid/AdminGridQueryEndpoint.cs`
2. Delete empty `Admin/Grid` directory
3. Update exact Host/Admin allowlists/counts **19 → 18** (Canon001–009, CanonicalCertification, Panel CERT guard Grid existence)
4. Focused Host tests only
5. SoT: Host/Admin/Grid = HOST_ZERO / ABSENT; preserve Panel CERT + W4 impl SHA semantics
6. No runtime route/behavior change

Do NOT open Admin/Access.
Do NOT invent Grid BFF module.
automaticNextImplementationTask remains NONE until Architect issues W1.
