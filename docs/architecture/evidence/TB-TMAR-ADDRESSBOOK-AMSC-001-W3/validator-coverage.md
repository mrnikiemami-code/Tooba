# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — validator-coverage

`ValidatorCoverageState = EXHAUSTIVE_5_REQUIRED_1_NO_VALIDATOR_REQUIRED`.

Durable guard: `AddressBookValidatorCoverageGuardTests` (endpoint-reachable request inventory +
validator resolution through the real CQRS foundation DI).

## Classification

| Request | Classification | Validator type |
| --- | --- | --- |
| `ListCustomerAddressesQuery` | `NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT` | — |
| `GetCustomerAddressQuery` | `VALIDATOR_REQUIRED_PRESENT` | `GetCustomerAddressQueryValidator` |
| `CreateCustomerAddressCommand` | `VALIDATOR_REQUIRED_PRESENT` | `CreateCustomerAddressCommandValidator` |
| `UpdateCustomerAddressCommand` | `VALIDATOR_REQUIRED_PRESENT` | `UpdateCustomerAddressCommandValidator` |
| `DeleteCustomerAddressCommand` | `VALIDATOR_REQUIRED_PRESENT` | `DeleteCustomerAddressCommandValidator` |
| `SetDefaultCustomerAddressCommand` | `VALIDATOR_REQUIRED_PRESENT` | `SetDefaultCustomerAddressCommandValidator` |

`5 + 1 = 6` = the full endpoint-reachable inventory. No unclassified request.

## `NO_VALIDATOR_REQUIRED` justification

`ListCustomerAddressesQuery` carries **no transport input** — it takes only the trusted server-side actor
identity from the session seam. Its durable reason is asserted by
`Complete_endpoint_inventory_is_exactly_6_with_5_required_and_1_no_validator`, which pins
`ListCustomerAddressesQuery` as the single `NO_VALIDATOR_REQUIRED` entry, and by
`Five_required_validators_resolve_via_foundation_DI_and_one_has_none`, which asserts that no validator is
registered for it.

## Discoverability

`Five_required_validators_resolve_via_foundation_DI_and_one_has_none` builds a real `ServiceCollection`,
calls `AddToobaCqrsFoundation(typeof(IAddressBookDirectory).Assembly)` and asserts that for each required
request `IValidator<TRequest>` resolves to exactly the expected validator type — i.e. validators are
discovered through the normal DI/MediatR validation pipeline, not by hand-wiring.

## Stable machine codes, not localized text

Validators emit **stable machine codes** via `.WithErrorCode(...)`:

| Validator | Codes used |
| --- | --- |
| `CreateCustomerAddressCommandValidator` | `RecipientNameRequired`, `ContactMobileRequired`, `CityNameRequired`, `PostalCodeRequired`, `PostalAddressRequired` |
| `UpdateCustomerAddressCommandValidator` | `AddressIdRequired` + the same five input-shape rules |
| `DeleteCustomerAddressCommandValidator` | `AddressIdRequired` |
| `SetDefaultCustomerAddressCommandValidator` | `AddressIdRequired` |
| `GetCustomerAddressQueryValidator` | `AddressIdRequired` |

No localized string is used as a validation code. `SafeErrorMapper.MapValidation` groups
`ValidationException.Errors` by property and emits the machine codes, resolving the envelope through the
catalogued `validation.failed` descriptor.

## Cross-cutting rules ownership

`AddressBookFluentRules.RequireNonBlank` / `RequireId` are the shared helpers; the codes live in
`AddressBookValidationCodes` in the same `Application/Validators/` folder. This matches the already
certified sibling pattern (`AccessControlValidationCodes`, `ContentValidationCodes`), which is why that
folder legitimately survives the W2 flattening as a structural (non-request) folder.

## Coverage guard still green after the AMSC waves

- W1 repaired the stale `preCertModules` assertion (W0 F8) without weakening the AddressBook assertions.
- W2 consolidated the physical-layout regex guard into `AddressBookPhysicalStructureGuardTests` so the
  validator-coverage guard owns only request/validator coverage.
- W3 adds `AddressBookModuleAmsc001W3CertGuardTests`; it does not touch validator coverage.

Result: `AddressBookValidatorCoverageGuardTests` — 6 tests, all PASS.
