# Permissions (TB-P09-T001)

After `AdminPanelAccess`, effective platform permissions via `IAccessControlDirectory.GetEffectiveAccessAsync(PlatformScope)`.

If actor has any order/return/fulfillment/refund family grants → require specific permission.
Else legacy admin (tenant#view only) → allow all projected ops.
UI shows only API-returned actions (no client-side permission matrix).
