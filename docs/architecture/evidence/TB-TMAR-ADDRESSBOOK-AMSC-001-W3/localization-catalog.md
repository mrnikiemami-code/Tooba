# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — localization-catalog

`LocalizationState = CANONICAL`. `HardcodedTextState = ZERO`. `DuplicateErrorDescriptorState = ZERO`.

## Canonical mechanism chain

```text
Domain/Infrastructure raises SemanticException(new SemanticError(<AddressBookErrorCodes.X>))
  -> AddressBookOperation catches it and returns Result.Failure(error)
    -> endpoint returns api.From(result) / api.FromFailure(error)
      -> SafeErrorMapper.Map(error) looks up IErrorDefinitionCatalog by code
        -> AddressBookErrorCatalogContributor supplies the ErrorDescriptor
          -> IErrorMessageLocalizer + AddressBookErrorResourceSet (.resx / .fa.resx) render the text
```

Every link is an existing repository mechanism. No parallel mapper, no endpoint-side status guessing, no
`exception.Message` as a contract.

## Owned code inventory (13 constants)

`Tooba.AddressBook.Contracts/Errors/AddressBookErrorCodes.cs`:

| Constant | Code | Ownership |
| --- | --- | --- |
| `AddressMissing` | `customer.address.missing` | AddressBook |
| `SessionRequired` | `customer.session.required` | **Foundation** (shared cross-cutting; consumed, not registered) |
| `ActorRequired` | `customer.address.actor_required` | AddressBook |
| `RecipientNameRequired` | `customer.address.recipient_name_required` | AddressBook |
| `ContactMobileInvalid` | `customer.address.contact_mobile_invalid` | AddressBook |
| `CountryInvalid` | `customer.address.country_invalid` | AddressBook |
| `ProvinceNameInvalid` | `customer.address.province_name_invalid` | AddressBook |
| `CityNameInvalid` | `customer.address.city_name_invalid` | AddressBook |
| `PostalCodeInvalid` | `customer.address.postal_code_invalid` | AddressBook |
| `PostalAddressInvalid` | `customer.address.postal_address_invalid` | AddressBook |
| `BuildingUnitInvalid` | `customer.address.building_unit_invalid` | AddressBook |
| `LabelInvalid` | `customer.address.label_invalid` | AddressBook |
| `RecipientNamePartsInvalid` | `customer.address.recipient_name_parts_invalid` | AddressBook |

- 13 constants, 13 distinct code values (no duplicate literal).
- **12** are owned and registered by AddressBook; **1** (`customer.session.required`) is owned by the
  Foundation contributor and deliberately **not** re-registered.

## Descriptor ownership — unique

`AddressBookErrorCatalogContributor` registers exactly the 12 owned codes through their constants:

| Code | Classification | HTTP |
| --- | --- | --- |
| `customer.address.missing` | `NotFound` | 404 |
| `customer.address.actor_required` | `Validation` | 400 |
| `customer.address.recipient_name_required` | `Validation` | 400 |
| `customer.address.contact_mobile_invalid` | `Validation` | 400 |
| `customer.address.country_invalid` | `Validation` | 400 |
| `customer.address.province_name_invalid` | `Validation` | 400 |
| `customer.address.city_name_invalid` | `Validation` | 400 |
| `customer.address.postal_code_invalid` | `Validation` | 400 |
| `customer.address.postal_address_invalid` | `Validation` | 400 |
| `customer.address.building_unit_invalid` | `Validation` | 400 |
| `customer.address.label_invalid` | `Validation` | 400 |
| `customer.address.recipient_name_parts_invalid` | `Validation` | 400 |

Asserted by `AddressBookModuleAmsc001W3CertGuardTests.AddressBook_owns_exactly_one_error_descriptor_per_owned_code`:

- every locally-owned constant appears in the contributor;
- `AddressBookErrorCodes.SessionRequired` does **not** appear (no duplicate owner);
- the contributor uses no `Contains(` / `StartsWith(` heuristic;
- **no other `*ErrorCatalogContributor.cs`** anywhere under `src/backend` registers any
  `customer.address.*` / `customer.session.required` literal.

Duplicate-suppression mechanisms (`first wins`, `last wins`, overwrite, `DistinctBy`, catch-and-ignore)
are absent. The composed catalog is additionally protected by the existing
`ErrorCatalogUniqueCodeGuardTests` and by `ErrorDescriptor` itself, which throws
`duplicate_error_descriptor:<code>` on duplicate registration.

## Resource coverage — both cultures

`AddressBookErrorCatalogContributor` sets `LocalizationKey = code`, and
`AddressBookErrorResourceSet` is registered as `IErrorResourceSet` in `AddressBookEndpointModule`.

| Culture file | Owned keys |
| --- | --- |
| `Resources/AddressBookErrors.resx` | 12 |
| `Resources/AddressBookErrors.fa.resx` | 12 |

Asserted by `AddressBookModuleAmsc001W3CertGuardTests.AddressBook_error_codes_resolve_to_localized_resources_in_both_cultures`:
every owned code has a `<data name="<code>">` entry in **both** the neutral and the Persian `.resx`.

`customer.session.required` is intentionally absent from the module resx — it is rendered by the
Foundation resource set that owns it.

## Localization hygiene

| Check | Result |
| --- | --- |
| `IErrorMessageLocalizer` used for rendering (not hand-built messages) | PASS (canonical stack) |
| endpoint-level `Accept-Language` parsing | ZERO |
| hard-coded Persian/English user-facing strings in Domain/Application/Endpoints/Infrastructure | ZERO (the W0 F2 defect: 14 raw `InvalidOperationException` Persian sites, all replaced in W1) |
| `exception.Message` / `ex.Message` used as a localized/user-facing contract | ZERO |
| existing keys renamed/repurposed | NONE (`customer.address.missing` and `customer.session.required` semantics preserved; 11 keys added in W1) |
| `StatusCodes` in endpoints | ZERO (only inside the catalog contributor, which is where status belongs) |

The remaining Persian literals in the module are in `Infrastructure/Adapters/AddressBookDevelopmentSeed.cs`
(developer demo data: recipient names, cities, streets, labels) — not user-facing error text, not rendered
through the error pipeline. Recorded as residual watch R3.

## Composed-catalog constructibility

`ErrorCatalogUniqueCodeGuardTests` (4 tests) and `AddressBookCanonicalPresentationGuardTests` (3 tests)
both PASS, confirming the composed `IErrorDefinitionCatalog` is constructible with no duplicate machine-code
descriptors.
