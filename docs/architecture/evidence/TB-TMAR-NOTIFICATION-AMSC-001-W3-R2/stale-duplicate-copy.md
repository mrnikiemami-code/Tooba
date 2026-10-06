# TB-TMAR-NOTIFICATION-AMSC-001-W3-R2 — stale-duplicate-copy

Post-move stale/duplicate audit (Structure skill §20):

## Checked

1. The ten emptied leaf directories were deleted after `git mv`:
   - `Application/Customer/Commands/{MarkCustomerNotificationRead,MarkAllCustomerNotificationsRead,DismissCustomerNotification}/`
   - `Application/Customer/Queries/{ListCustomerNotifications,GetCustomerUnreadNotificationCount}/`
   - `Application/Seller/Commands/{MarkSellerNotificationRead,MarkAllSellerNotificationsRead,DismissSellerNotification}/`
   - `Application/Seller/Queries/{ListSellerNotifications,GetSellerUnreadNotificationCount}/`
   — none exists on disk (`Stale_use_case_leaf_directories_are_absent` guard).
2. Leftover `Application/Customer/Validators/` and `Application/Seller/Validators/` shells (created in
   W2, never populated) were found empty and deleted.
3. `git ls-files` + `git status` show only `R` (rename) entries for the 16 axis files — no leftover
   old-path copies remain tracked; each type has exactly one live physical home.
4. Repo-wide source scan for old namespace segments
   (`Application.(Customer|Seller).(Commands|Queries).<UseCase>`) returns zero hits in production,
   tests, or Host after the reference repoints.
5. Solution Explorer: `Tooba.slnx` groups the module by `.csproj` only (no per-file solution entries),
   so no solution entry could point at a deleted path — `/Modules/Notification/` grouping unchanged.

## Result

Physical-Copy-State = `CLEAN` — one authoritative physical home per responsibility, no stale path,
no dual live home.
