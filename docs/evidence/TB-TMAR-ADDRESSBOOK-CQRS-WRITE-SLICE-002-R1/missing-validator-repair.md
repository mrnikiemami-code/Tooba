# Repair: Missing Delete + SetDefault Transport Validators

Task: `TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1`
Parent: `TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002` at `92a7f9688f72e56cfe9c108bbcca6ff09ab49ac6`
Architect verdict on parent: `PARTIAL_ACCEPTANCE_ONLY`.
AddressBook remains **IN_PROGRESS**; **NOT** `COMPLETE_REFERENCE_PATTERN`; **NOT** `STRUCTURE_CERTIFIED`.

## 1. Parent discrepancy (honest root cause)

The parent task `...-CQRS-WRITE-SLICE-002` reported two Delete/SetDefault validators as delivered in its Result, but **the parent commit contained none of them**:

```
git ls-files src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Validators
→ AddressBookFluentRules.cs
→ Customer/Create/CreateCustomerAddressCommandValidator.cs
→ Customer/Get/GetCustomerAddressQueryValidator.cs
→ Customer/Update/UpdateCustomerAddressCommandValidator.cs
```

The two validator files were present only as **untracked** working-tree files:

```
git status --porcelain src/backend/Modules/AddressBook
?? .../Validators/Customer/Delete/
?? .../Validators/Customer/SetDefault/
```

Root cause: in the parent task the endpoint-layer `git add` pathspecs did not include the `Validators/Customer/Delete` and `Validators/Customer/SetDefault` directories, so `git commit` recorded only the command/handler folders. The files themselves were correct; only the **commit** was incomplete. This repair commit closes that gap. No command/handler file needed any change.

## 2. Exact two validator files now committed

| File | Namespace | Status before | Status now |
| --- | --- | --- | --- |
| `src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Validators/Customer/Delete/DeleteCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.Customer.Delete` | present but untracked | tracked + committed |
| `src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Validators/Customer/SetDefault/SetDefaultCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.Customer.SetDefault` | present but untracked | tracked + committed |

Both files were written exactly as required and were **not** modified by this repair except for committing them.

## 3. Exact rule and error code

```csharp
public sealed class DeleteCustomerAddressCommandValidator : AbstractValidator<DeleteCustomerAddressCommand>
{
    public DeleteCustomerAddressCommandValidator()
    {
        AddressBookFluentRules.RequireId(this, x => x.AddressId, AddressBookValidationCodes.AddressIdRequired);
    }
}
```

```csharp
public sealed class SetDefaultCustomerAddressCommandValidator : AbstractValidator<SetDefaultCustomerAddressCommand>
{
    public SetDefaultCustomerAddressCommandValidator()
    {
        AddressBookFluentRules.RequireId(this, x => x.AddressId, AddressBookValidationCodes.AddressIdRequired);
    }
}
```

- Rule: exactly one per validator, `AddressId` only, via the existing `AddressBookFluentRules.RequireId` helper.
- Error code: the existing stable `AddressBookValidationCodes.AddressIdRequired` = `customer.address.id_required` (unchanged).
- `ActorUserId` is **not** validated in either validator (only mentions are XML-doc notes stating it is deliberately not validated).
- No new helper, no new validation code, no new business rule, no HTTP mapping, no error translation.

## 4. Validator coverage — 6-request inventory, gap ZERO

| # | Request | Kind | Route-derived input? | Classification | Validator present |
| --- | --- | --- | --- | --- | --- |
| 1 | `GetCustomerAddressQuery` | read | `AddressId` from route | `VALIDATOR_REQUIRED` | yes |
| 2 | `CreateCustomerAddressCommand` | write | body only (trusted actor) | `VALIDATOR_REQUIRED` | yes |
| 3 | `UpdateCustomerAddressCommand` | write | `AddressId` + body | `VALIDATOR_REQUIRED` | yes |
| 4 | `DeleteCustomerAddressCommand` | write | `AddressId` from route | `VALIDATOR_REQUIRED` | yes (this repair) |
| 5 | `SetDefaultCustomerAddressCommand` | write | `AddressId` from route | `VALIDATOR_REQUIRED` | yes (this repair) |
| 6 | `ListCustomerAddressesQuery` | read | trusted actor only | `NO_VALIDATOR_REQUIRED` | n/a |

Result: **6 requests total**, `VALIDATOR_REQUIRED = 5`, `NO_VALIDATOR_REQUIRED = 1`, **validators present = 5**, **validator gap = ZERO**. Verified by directory listing: exactly 5 `*Validator.cs` files under `Tooba.AddressBook.Application/Validators/`.

## 5. Host routes and Endpoints unchanged

- `git diff -- src/backend/Host/Tooba.Host/AddressBook/` is empty — `AddressBookEndpoints.cs` and `AddressBookDevelopmentSeed.cs` unchanged; Host still owns all six routes.
- `Program.cs`, `AddressBookEndpointModule.cs`, AddressBook routes, root structure, schema/migrations and frontend untouched.
- `Tooba.AddressBook.Endpoints` still maps **ZERO** routes (`grep Map*` → no matches).
- No route moved; endpoint migration not started (as required by this repair).

## 6. Focused build results

| Build | Result |
| --- | --- |
| `dotnet build …/Tooba.AddressBook.Application.csproj --no-restore` | **Build succeeded**, 0 warnings, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | **Build succeeded**, 0 errors, 11 pre-existing warnings — none introduced |

## 7. Focused tests

**None run.** No directly relevant tiny validator guard exists (no AddressBook test project; the Host-side `AddressBookFoundationTests` target Host endpoint/seed behavior, not Application validators). Per task instruction, no tests were added or run.

## 8. Production scope

**Limited to exactly two validator files** plus the two canonical docs artifacts. No command, handler, Host, Program, Endpoints, root, schema or frontend file changed in this repair. No test code changed.

## 9. Next recommended slice

**`TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-SLICE-001`** (unchanged from the parent recommendation): migrate only the two read routes (`GET /v1/customer/addresses`, `GET /v1/customer/addresses/{addressId:guid}`) into `Tooba.AddressBook.Endpoints/Customer/` through `ISender`, introduce the Host-independent actor seam on `Tooba.BuildingBlocks.Security.ICurrentAuthenticatedUser` with the guest value sourced from `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId`, and leave the remaining four routes Host-owned for a following slice.
