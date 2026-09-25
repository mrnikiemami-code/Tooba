# TMAR Host Evacuation Protocol

Status: CANONICAL
Protocol: BRIDGE-WAKE-V1
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

## Purpose

The TMAR recovery objective is not merely to make architecture guards pass. The Host must be reduced to a thin platform/composition shell while module-specific behavior is rehomed to its real architectural owner with semantic preservation.

After the active Fulfillment wave is completed, TMAR proceeds through `src/backend/Host/Tooba.Host` **folder-by-folder and file-by-file**, following the actual Host tree rather than selecting the next module only from the uncertified-module list.

Canonical traversal begins from the top of the Host tree (for example AccessControl, then AddressBook, then subsequent folders in repository order) and continues until every Host production file has been dispositioned.

## Non-negotiable semantic-preservation rule

No Host production file may be deleted merely because it is labelled dead, residue, legacy, thin, or zero-consumer.

Before deleting a Host production file:

1. Create a **Content Disposition Map**.
2. Inspect every type, record, interface, member, method, constant, state holder, endpoint, policy, integration call, error mapping, registration, and runtime behavior in the file.
3. Assign each live responsibility to its correct architectural owner.
4. A single Host file may be split across multiple modules/layers.
5. Rehome all live responsibilities before removing the Host shell.
6. Reconnect DI/call sites and prove focused semantic/runtime parity.
7. Only then may the evacuated Host file be removed.

A syntax-only artifact may be removed without rehome only when explicit evidence proves:
- zero production consumers,
- no behavior,
- no contract/state authority,
- underlying canonical types remain,
- no compatibility alias/shim is required.

## Folder-by-folder recovery algorithm

For each Host folder:

1. Inventory every production file.
2. Read every file completely.
3. Produce a member-level Content Disposition Map.
4. Determine destination owner(s).
5. If the destination module is not structurally ready, repair that destination first:
   - module-owned Endpoints,
   - MediatR 12.5 CQRS,
   - Commands/Queries/Handlers,
   - FluentValidation only for untrusted transport shape,
   - Contracts-only cross-module boundaries,
   - capability/integration foldering,
   - exact path/namespace alignment,
   - Host dependency = ZERO.
6. Move/rehome content in bounded slices.
7. Prove semantic parity with focused tests/guards.
8. Remove only the now-empty/evacuated Host shell.
9. Re-scan the folder; folder closure requires no misplaced module-specific responsibility.
10. Move to the next Host folder.

Do not skip a Host file because its corresponding module was previously marked COMPLETE_REFERENCE_PATTERN or STRUCTURE_CERTIFIED. Host evacuation may reopen ownership review without invalidating unrelated accepted structure locks.

## Host end-state

Host may retain only genuine platform/composition responsibilities, such as:
- process startup/composition root,
- DI composition,
- middleware,
- generic session/current-user plumbing,
- tenant/platform context plumbing,
- generic authorization infrastructure adapters,
- generic environment/runtime seams,
- health/observability/platform hosting,
- migration/development bootstrap only where it is truly composition/infrastructure and contains no module business authority.

Host must not own module-specific:
- endpoints,
- business/application policies,
- CQRS handlers,
- grid policies,
- domain decisions,
- persistence authority,
- module-specific authorization policy/projection,
- module-specific workers when the module can own them,
- module-specific aliases/shims,
- cross-module orchestration that belongs to a module/application boundary.

## Current recovery checkpoint (2026-09-25)

Accepted/verified recent work before this protocol:
- Payment: ARCH-COMPLETE-002 STRUCTURE_CERTIFIED.
- Settlement audit classification repaired to 4 required / 6 no-validator-required.
- Settlement pre-cert validators completed.
- Settlement: ARCH-COMPLETE-002 STRUCTURE_CERTIFIED at `54b1c8ff1f6e9214a5b5c16b6103f0285bd2a37e`, SoT stamp `01d15f3cb1ad38f0e91ed65e990e32c4f9d19876`.
- Fulfillment audit completed and R1 corrected validator taxonomy to 15 endpoint requests = 10 VALIDATOR_REQUIRED + 5 NO_VALIDATOR_REQUIRED.
- Fulfillment pre-cert repair accepted at `16062d45bde71476da35f9e20622f1b6b5637fa8`: 10 transport validators added; 15/10/5 coverage locked; `FulfillmentReturnsGridAliases.cs` removed only after zero-consumer proof. The file contained aliases only and its underlying Fulfillment/Returns types remained intact.
- Fulfillment structure certification is deliberately deferred until Fulfillment-specific Host files are fully evacuated.
- Current Host Fulfillment-specific files identified for evacuation:
  - `Admin/HostFulfillmentAdminAuthorizer.cs`
  - `Customer/HostFulfillmentCustomerAuthorizer.cs`
  - `Seller/HostFulfillmentSellerAuthorizer.cs`
- The active intended next task is `TB-TMAR-FULFILLMENT-HOST-EVACUATION-001`, then Fulfillment structure certification.

## Known post-Fulfillment Host traversal rule

After Fulfillment closure, do NOT resume the old "next uncertified module" sequence automatically.

Traverse Host folders in repository/Solution Explorer order. Example:
- AccessControl
- AddressBook
- Admin
- Authentication
- Authorization
- Caching
- Composition
- Configuration
- Content
- Customer
- CustomerProfile
- Development
- ...continue until Host is exhausted.

For each folder, finish every file before moving to the next folder.

Example: AccessControl currently contains:
- `AccessControlDemoSnapshot.cs`
- `AccessControlDevelopmentSeed.cs`
- `AccessControlEndpoints.cs`

These must be decomposed by responsibility. `AccessControlDevelopmentSeed.cs` touches multiple module owners, so it must not simply be moved wholesale into AccessControl; its responsibilities may split among AccessControl, Catalog, Identity, Party, Offer, Inventory, Cart, Order, Pricing, Tax, and/or a generic Development orchestration boundary.

Example: AddressBook currently contains:
- `AddressBookDevelopmentSeed.cs`
- `AddressBookEndpoints.cs`

Both must receive the same file-by-file/member-by-member treatment.

## Validation philosophy

Tests are evidence, not the navigation strategy.

Priority order:
1. find all misplaced Host content,
2. map ownership,
3. rehome without semantic loss,
4. then run the smallest focused validation needed.

Do not spend broad test time while obvious Host residue remains.
No broad/full suites unless explicitly required by a blocker.

## Recovery instruction for a new chat

When recovering TMAR context:
1. read `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`
2. read `docs/architecture/tmar-current-state.json`
3. read `docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md`
4. read the latest task/result/evidence for the active Host folder/module
5. continue from the current Host folder; do not revert to module-list sequencing
