# TB-TMAR-NOTIFICATION-AMSC-001-W3-R2 — certification-drift

Architect finding (Bridge Task `TB-TMAR-NOTIFICATION-AMSC-001-W3-R2`): CURRENT CERTIFICATION IS NOT
ACCEPTED AS FINAL.

1. **W2 `READY_FOR_CERTIFY` was incorrect/stale** under the current
   `.cursor/skills/tooba-architecture-structure/SKILL.md`: the skill's §8 single-file leaf rule and
   §11 depth rules reject use-case leaf folders without recorded complexity justification, yet the
   W2 handoff certified a one-folder-per-use-case tree as `PROFESSIONAL_SHALLOW`. The exact
   single-file leaf paths found at starting HEAD `7b8ab79e`:
   - `Application/Customer/Commands/MarkAllCustomerNotificationsRead/` (1 production file)
   - `Application/Customer/Queries/GetCustomerUnreadNotificationCount/` (1 production file)
   - `Application/Seller/Commands/MarkAllSellerNotificationsRead/` (1 production file)
   - `Application/Seller/Queries/GetSellerUnreadNotificationCount/` (1 production file)
   plus six more use-case leaves carrying only request + validator
   (`MarkCustomerNotificationRead`, `DismissCustomerNotification`, `ListCustomerNotifications`,
   `MarkSellerNotificationRead`, `DismissSellerNotification`, `ListSellerNotifications`).
2. **W3 depended on the stale Structure handoff**: the W3 certification guard
   (`NotificationModuleAmsc001W3CertGuardTests`) hard-coded the over-foldered leaf paths as its
   "ten endpoint requests" proof, so certification passed on the stale shape.
3. **W3 historical record remains preserved**: `tmar-current-state.json`
   (`notificationModuleAmsc001W3`), the manifest entry, `TOOBA-TMAR-MASTER-RECOVERY.md` and
   `docs/architecture/evidence/TB-TMAR-NOTIFICATION-AMSC-001-W3/` are NOT rewritten; they stay as
   historical truth of what W3 recorded. W3-R2 changes only the current physical structure.
4. **Current repaired tree requires a separate Certify**: after this Structure repair the
   Application request tree satisfies the skill (ZERO child directories under the four axes,
   `PROFESSIONAL_SHALLOW`, `EXACT` path↔namespace, `ENFORCED` root allowlists, `CANONICAL` solution
   grouping, `COHESIVE` files), so `Structure-State = READY_FOR_CERTIFY`; issuing any certification
   verdict is explicitly out of scope for this Structure task and is left to the next Certify wave.
5. **W3-R1 final SHA recovery gap remains pending**: `notificationModuleAmsc001W3R1` records its
   SoT block but the Master Recovery reconciliation for its own final commit SHA `7b8ab79e` has not
   been recorded (the W3-R1 Master-Recovery section and SoT block do not carry `7b8ab79e` as the
   recorded R1 commit). Per the task, that gap is deliberately NOT repaired here; it is recorded as
   pending follow-up: `recoveryFollowupRequired = W3_R1_FINAL_SHA_7B8AB79E_NOT_YET_RECORDED` in the
   additive SoT block `notificationModuleAmsc001W3R2`, to be reconciled by the post-Certify recovery
   step.
