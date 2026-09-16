# Access Control audit — TB-P11-T001

Policy: PostgreSQL SoT + SpiceDB enforcement (`AccessControlDirectory`). Catalog module/action keys in `PermissionCatalog.cs`. Scope kinds Global/Category/Product/Brand/Warehouse/Store/OrderSegment. Seller ceiling + non-delegable platform-only. Shared UX `access-control-center.tsx` with human labels (`permission-labels.ts`) and effective-permissions preview.

## Findings

| ID | Severity | Finding |
| --- | --- | --- |
| A1 | **BLOCKER** | `EnsureCapabilityAsync` fail-open for `accesscontrol.view` and `accesscontrol.manage` (`AccessControlEndpoints.cs`). Panel-authorized actors can mutate ACC without SpiceDB capability tuples. |
| A2 | HIGH | Admin panel gate is `tenant#view` only (`AdminPanelAccess.cs`). Dashboard/settings/languages ignore fine-grained ACC keys. |
| A3 | HIGH | `access_audit_events` written; no audit API/UI on `/admin/access-control`. |
| A4 | HIGH | Nav `itemAllowed` treats `caps===null` as allow-all (`admin-shell.tsx`). Capability fetch failure shows every live item. |
| A5 | MEDIUM | No explicit deny grants; allow-list + seller ceiling only. |
| A6 | MEDIUM | Warehouse/Store/OrderSegment scope APIs deferred empty. |
| A7 | MEDIUM | ACC auth call context hardcodes `ToobaEdition.SingleStore`. |
| A8 | MEDIUM | Residual permission-key exposure: search matches keys, EN fallback `Permission ${id}`, `data-permission`. Primary copy is humanized. |
| A9 | LOW | Pages hardcode `canManage=true`. |

Do not redesign SpiceDB in T001. Repair fail-open is the first P11 implementation candidate.
