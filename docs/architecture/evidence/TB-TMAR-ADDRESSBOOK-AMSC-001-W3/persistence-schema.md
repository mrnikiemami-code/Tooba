# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — persistence-schema

`PersistenceOwnershipState = CORRECT`. `SchemaMigrationState = UNCHANGED`.
`MigrationFilesChanged = 0`. `SchemaChange = NONE`.

## Ownership

| Concern | Owner | Evidence |
| --- | --- | --- |
| DbContext | `Tooba.AddressBook.Infrastructure/Persistence/AddressBookDbContext.cs` | one DbContext for the module |
| Schema | `address_book` (`AddressBookDbContext.Schema`) | module-owned schema constant, also used by `AddressBookOutboxRegistration.Schema` |
| Entity configuration | `Persistence/Configurations/CustomerAddressConfiguration.cs` | `IEntityTypeConfiguration<CustomerAddress>` |
| Migrations | `Persistence/Migrations/` | module-owned |
| Outbox registration | `Outbox/AddressBookOutboxRegistration.cs` | `IOutboxModuleRegistration` bound to `AddressBookDbContext` + `address_book` schema |
| DI | `DependencyInjection/AddressBookModule.cs` | registers `IAddressBookDirectory → AddressBookDirectory` |

## Migration inventory — unchanged by AMSC

```text
20260825171858_InitialAddressBook.cs
20260825171858_InitialAddressBook.Designer.cs
20260913180000_AddRecipientNameParts.cs
AddressBookDbContextModelSnapshot.cs
```

Asserted by `AddressBookModuleAmsc001W3CertGuardTests.AddressBook_schema_and_migrations_are_unchanged`,
which pins the exact file set (sorted) — PASS.

| Safety check | Result |
| --- | --- |
| new migration added by W0–W3 | NO |
| migration identifier changed | NO |
| migration order changed | NO |
| `Up`/`Down` semantics changed | NO |
| snapshot semantics changed | NO |
| tables / columns / indexes / constraints changed | NO |
| transaction behaviour changed | NO |
| migrations regenerated for structural cleanup | NO |

W1 changed only C# fault types and handler return types (no EF model change). W2 changed only file paths
and namespaces (the Application project has no EF artifacts). W3 is verification only.

## No cross-module persistence

| Check | Result |
| --- | --- |
| foreign `DbContext` injected into the module | ZERO |
| foreign `DbSet` accessed | ZERO |
| foreign schema/table read | ZERO |
| cross-module FK | ZERO |
| `DbContext` access from Application or Endpoints | ZERO |
| cross-module SQL/EF join | ZERO |

`AddressBookDirectory` touches only its own `Addresses` set. `ARCH-DATA-001` intact.

## Schema-migration drift check

`docs/architecture/evidence/.../migrate.md` (W1) recorded `Schema-Migration-State = UNCHANGED` and required
that W1 must not regenerate migrations. Verified at W3: the migration file set and contents are identical to
the W0 baseline. The only Infrastructure file changed across the whole AMSC run is
`Adapters/AddressBookDirectory.cs` (fault types in W1).

## Persistence boundary of the fault repair

W1's `SemanticException` conversion in `AddressBookDirectory.cs` replaced three
`InvalidOperationException` throws. It did **not** change:

- the EF query shape (still `Addresses.Where(a => a.Id == id && a.OwnerUserId == actor)`),
- the transaction/`SaveChangesAsync` behaviour,
- the "single default per owner" invariant handling,
- the returned `CustomerAddressRecord` projection.

Behaviour preservation for the persistence path is confirmed by `AddressBookFoundationTests` passing
unchanged and by the (Testcontainers-gated) `AddressBookPostgresTests` remaining in their pre-existing
SKIP state (no Postgres in this environment).
