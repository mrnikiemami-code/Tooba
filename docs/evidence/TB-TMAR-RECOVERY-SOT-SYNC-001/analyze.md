# Analyze — TB-TMAR-RECOVERY-SOT-SYNC-001

Track: RECOVERY_SOT_CLOSURE
Parent: TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Scope: recovery/documentation only — ZERO production-code change

## 1. Authoritative files inspected

| File | Role |
| --- | --- |
| `docs/architecture/tmar-current-state.json` | machine-readable TMAR SoT |
| `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` | Master Recovery narrative |
| `docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md` | architect recovery bootstrap |
| `docs/ai/TOOBA-RECOVERY-CONTEXT.md` | chat-recovery context |
| `docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md` | evacuation protocol |
| `docs/architecture/tmar-module-structure-manifests.json` | ARCH-COMPLETE-002 manifest |
| `src/backend/Host/Tooba.Host.Tests/TmarDurableGuardTests.cs` | durable recovery guard |

Git history points inspected around `7d8ea211` (Root Global Boundaries R3 SoT stamp),
`c63f6ebb` (R3 implementation), `498c46bd` (Authorization post-cert implementation),
and `736f23d3` (Authorization post-cert docs-only SoT stamp).

## 2. Observed good state (already true on main)

- `lastAcceptedTask` already recorded `TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001`.
- `lastAcceptedCommit` already recorded the implementation commit `498c46bd...`.
- Root Global Boundaries R3 lineage (`c63f6ebb...` implementation / `7d8ea211...` stamp) was recorded.

## 3. Observed defects (the actual synchronization debt)

1. `lastAcceptedSoTStamp` was absent at the top level. The later docs-only acceptance stamp
   commit `736f23d3` (created after `498c46bd`) was not distinguishable from the implementation
   commit, creating an implicit conflation risk.
2. `currentHostEvacuation` still carried the historical AddressBook/Content sequencing narrative in
   its *current* fields (`workflowStop = USER_REVIEW_ADDRESSBOOK_CHECKPOINT` at line ~1039 and the
   Content R4 "Current Live State" heading), even though all Host folders were long closed.
3. `TOOBA-TMAR-MASTER-RECOVERY.md` had **no** authoritative "Latest Accepted TMAR Checkpoint" head
   section; a fresh architect hitting the first `Next task:` line would resume a historical folder.
4. `TOOBA-ARCHITECT-BOOTSTRAP.md` presented `USER_REVIEW_ADDRESSBOOK_CHECKPOINT` and
   `USER_REVIEW_REQUIRED_AFTER_ADDRESSBOOK_CERTIFICATION_STOP` as *current* next task/gate, and its
   later summary block still said "Fulfillment … NOT certified, pre-cert validator repair required next".
5. `docs/ai/TOOBA-RECOVERY-CONTEXT.md` had "Current Issued Task = TB-TMAR-RECOVERY-LOCK-HARDEN-001"
   and no reconciliation section at all.
6. Master Recovery's Content R4 section was still titled *"Current Live State (Content R4)"* and its
   AddressBook section pointed at `TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001` as "the current next task".
7. The durable guard `Recovery_current_state_is_fresh_and_machine_readable` still asserted the
   AddressBook-era `lastAcceptedTask` and a 9-module `structureLock.certifiedModules` set.

## 4. Historical pointers that must never become current again

- Fulfillment (`TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001` / its pre-cert repair)
- AddressBook (`TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001`, `TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001`)
- Authentication (`TB-TMAR-HOST-AUTHENTICATION-SPLIT-001` and the identity option decision)
- Admin (`TB-TMAR-HOST-ADMIN-CANON-*`, Host-Admin AMC waves)
- Root Global Boundaries R2/R3
- Payment pre-cert (`…PRECERT-VALIDATION-001`, `…PRECERT-DIRECTORY-SPLIT-001-R1`)

## 5. Conclusion

The defect is a **pointer/semantics** defect, not a structural one. All architecture facts are already
accepted and must be preserved verbatim as history. The repair is limited to: making
`TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001` the unambiguous current checkpoint, disambiguating
implementation vs docs-only stamp commits, removing stale *current* AddressBook/Content pointers, and
adding an explicit stop state with no automatic next implementation task.

No production file, project file, route, schema, migration or module manifest requires change.
