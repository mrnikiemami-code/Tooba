# TB-TMAR-ADDRESSBOOK-AMSC-001-W2 — cohesion-balance

`File-Cohesion-State = COHESIVE`.

## File sizes after W2 (hand-written production sources)

| File | Approx. LOC | Responsibility | Verdict |
| --- | --- | --- | --- |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | ~220 | EF adapter for the address book (create/update/delete/set-default/read + owner check + recipient composition) | `OVERSIZED_ONLY` → not applicable (under the 800 threshold) |
| `Domain/Aggregates/CustomerAddress.cs` | ~230 | `CustomerAddress` aggregate + invariants | COHESIVE |
| `Application/Composition/AddressBookOperation.cs` | ~40 | Fault→Result composition seam | COHESIVE |
| `Application/Validators/AddressBookFluentRules.cs` | ~40 | Shared validation codes + Fluent helpers | COHESIVE |
| `Application/Models/CustomerAddressWrite.cs` | ~15 | Application write input record | COHESIVE |
| `Application/Ports/IAddressBookDirectory.cs` | ~25 | Module port | COHESIVE |
| each `Addresses/Commands/*.cs` | ~20 | Request record + handler | COHESIVE |
| each `Addresses/Queries/*.cs` | ~30 | Request record + handler | COHESIVE |
| each `Addresses/Validators/*.cs` | ~25 | Transport validator | COHESIVE |

No file exceeds the 800-LOC hand-written threshold (`TmarSourceSizeGuard`); AddressBook has **no entry** in
`Baselines/tmar-source-size-baseline.json`. W2 did not change any file's size meaningfully (namespace + using
lines only).

## God-file / over-split balance

| Anti-pattern | Present? | Note |
| --- | --- | --- |
| Mixed `*Contracts.cs` Application dump | No | Contracts holds only `Dtos/`, `Errors/`, `Ports/` |
| Unrelated multi-responsibility god-file | No | Largest file is a single-responsibility EF adapter |
| One folder per request with one file | **Removed by W2** | Was 11 leaves; now 0 |
| Cosmetic split to game size/structure guards | No | W2 **merged** directories, it did not split files |

W2 deliberately did **not** split any file. The temptation to "fix" the single-file leaf folders by
splitting request and handler into separate files would have been a cosmetic split (hard rule 7) — it was
rejected in favour of flattening, which is the canonical repair.

## Behavioural surface untouched

W2 is a **pure structure + namespace move**. No type was renamed, no signature changed, no logic touched,
no DI registration changed, no route changed, no DTO changed, no schema/migration regenerated. Diff review
confirms every moved file differs only in its `namespace` line and (for validators/endpoints) its `using`
lines.
