# Manifest-Structure (section 23)

File: `docs/architecture/tmar-module-structure-manifests.json`

## 1. Changes made by W2

```diff
   "uncertifiedHttpOwningModules": [
-    "Returns",
     "Support",
     "Wallet"
   ],
-  "preCertModules": []
+  "preCertModules": [
+    { "module": "Returns", "structureCertified": false, "lockVersion": "ARCH-COMPLETE-002", … }
+  ]
```

| Change | Justification |
|---|---|
| `"Returns"` removed from `uncertifiedHttpOwningModules` | The module's physical structure is no longer *unknown/uncertified*: it is now an explicitly recorded pre-certification structure surface with allowlists. |
| Returns added to `preCertModules` with `structureCertified: false` | Section 23: update physical allowlists/forbidden lists honestly for the touched module, and **do not** flip whole-module `structureCertified` outside a Certify task. |
| `lockVersion: "ARCH-COMPLETE-002"` | Keeps lock vocabulary aligned with the certified modules. |

`modules[]` (the 28-entry certified array) is **untouched** — no `Returns` entry added.
Promotion into `modules[]` is the exclusive authority of the W3 `tooba-architecture-certify` wave.

## 2. Manifest integrity after the edit

| Key | Value |
|---|---|
| `version` / `definitionMarker` / `rules` | unchanged |
| `modules` (certified) | 28 entries — `AccessControl, Identity, Media, OperatorProfile, ProductQnA, BulkInquiry, Wishlist, UserPreference, PageComposition, Catalog, Party, Localization, Order, Cart, StoreContext, Offer, Payment, Settlement, Story, Fulfillment, AddressBook, Content, CustomerProfile, Inventory, Notification, Pricing, ProductWorkspace, Promotion` |
| `uncertifiedHttpOwningModules` | `Support, Wallet` |
| `preCertModules` | `Returns` |

Diff footprint: `92 insertions(+), 2 deletions(-)` — one removal line, one replacement block. No
unrelated manifest entry was reformatted, reordered or rewritten.

## 3. Recorded physical contract for Returns

| Project | `rootAllowlist` | `forbiddenRootFiles` | `forbiddenTopLevelFolders` |
|---|---|---|---|
| `Tooba.Returns.Contracts` | `[]` | 4 entries | `[]` |
| `Tooba.Returns.Domain` | `[]` | 3 entries | `[]` |
| `Tooba.Returns.Application` | `[]` | 5 entries | `Commands, Queries, Models, Ports, Validators, Errors, Handlers, Requests` |
| `Tooba.Returns.Infrastructure` | `[]` | 5 entries | `Migrations, Repositories` |
| `Tooba.Returns.Endpoints` | `["ReturnEndpointModule.cs"]` | 4 entries | `[]` |
| `Tooba.Returns.Tests` | `[]` | `[]` | `[]` |

Every allowlist matches disk exactly (see `root-allowlist.md`); every forbidden entry is absent.
No allowlist was widened to hide debt — five of the six projects carry an **empty** allowlist and the
single non-empty entry is the justified module composition file.

## 4. Enforcement

`ReturnsModuleAmsc001W2StructureGuardTests`:

- `Root_allowlists_match_disk_and_forbidden_entries_are_absent` — manifest ↔ disk equality plus
  forbidden-file/folder absence;
- `Manifest_records_returns_as_pre_cert_not_certified` — `uncertifiedHttpOwningModules` no longer
  contains `Returns`; `preCertModules[Returns].structureCertified == false`; `lockVersion` is
  `ARCH-COMPLETE-002`; exactly 6 projects recorded; **and** `modules[]` still contains no `Returns`
  entry (a premature W3 promotion would fail the W2 guard).
