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

## Architect ↔ Cursor canonical task handoff

This is a recovery-critical execution contract. A new chat must restore it before issuing any TMAR implementation task.

### Architect task delivery

- ChatGPT acts as Architect; Cursor is the worker.
- Never paste the full task body into chat when a task is issued.
- Generate a real downloadable artifact at `/mnt/data/<Task-ID>.task.md` and present only the short status + download link in chat.
- The repository copy committed by Cursor must be exactly `docs/ai/tasks/<Task-ID>.task.md`.
- Filename must exactly equal `Task-ID + ".task.md"`.
- Repair tasks use canonical suffixes `-R1`, `-R2`, ...; do not invent unrelated naming patterns.
- Every task starts with `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1` / `BEGIN_TOOBA_TASK` and ends with `END_TOOBA_TASK`.
- Include Channel=`tooba-main`, WorkerId=`tooba-worker-01`, AgentType=`cursor`, Program, Mode, parent task/accepted commit when applicable, scope, protected state, focused validation, evidence, exact success criteria, canonical Result fields, and STOP/no-polling rules.
- Issue exactly ONE task at a time. Never issue the next task while Cursor is still working or before the prior Worker Result is verified and accepted/repaired.
- Prefer coherent family-sized slices, but keep target execution around 10–12 minutes and hard maximum 15 minutes.
- If a clean task cannot finish inside the hard limit, Cursor must return `INCOMPLETE` and `STOP`; never broaden scope, retry-loop, silently split, or auto-start the next task.
- Use focused builds/tests only. Tests are evidence, not navigation. No solution build, broad integration suite, broad architecture suite, or retry cascade unless a concrete blocker explicitly requires it.
- Avoid task proliferation: combine closely related routes/use-cases when the shared CQRS/boundary already exists and the whole family safely fits the hard timebox.

### Cursor worker result contract

Cursor returns only the canonical result envelope:

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: <exact task id>
Parent-Task: <when applicable>
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
...
END_TOOBA_WORKER_RESULT
```

The Result must include the exact Task-ID, status/verdict, structured state fields requested by the task, focused validations, evidence path, Git commit/push state, user-work-preserved state, recovery-next-task, and next-recommended-task, then stop completely.

### Architect result handling

For every Worker Result:
1. Verify the reported commit against the repository before accepting factual claims.
2. Verify the canonical task artifact and evidence exist on `main`.
3. Inspect only the code/files necessary to validate scope, ownership, boundaries and claimed behavior.
4. Decide `ARCHITECT-ACCEPTED` or issue a narrowly scoped repair only when a real defect/canonical mismatch exists.
5. Do not trust `Next-Recommended-Task` blindly; choose the next task from verified repository state.
6. After acceptance, issue at most one next downloadable `.task.md`.
7. Keep Recovery SoT synchronized frequently enough that a new chat can resume from repository state without conversational memory.

### Minimal user recovery phrase

If the user says only something equivalent to:

`برگردیم به TMAR؛ ریکاوری را انجام بده`

restore this contract plus the current SoT, latest accepted task/commit, active track/folder, known blockers/debt, and exact next task before continuing. Do not ask the user to reconstruct prior task history unless the repository evidence is genuinely missing.

## Recovery instruction for a new chat

When recovering TMAR context:
1. read `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`
2. read `docs/architecture/tmar-current-state.json`
3. read `docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md`
4. read the latest task/result/evidence for the active Host folder/module
5. continue from the current Host folder; do not revert to module-list sequencing
