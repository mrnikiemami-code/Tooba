# TB-TMAR-CART-AMSC-001-W0 — Manifest / Structure Guard State

## `Manifest-Structure-State = CERTIFIED_WITH_UNLOCKED_STRUCTURE`

## 1. Module manifest entry (`docs/architecture/tmar-module-structure-manifests.json`)

```json
{
  "module": "Cart",
  "structureCertified": true,
  "lockVersion": "ARCH-COMPLETE-002",
  "projects": [ Tooba.Cart.Application, Tooba.Cart.Endpoints, Tooba.Cart.Infrastructure ]
}
```

Line 964 of the manifest. Three defects:

| Defect | Detail |
| --- | --- |
| Incomplete project coverage | 3 of 5 production projects listed. `Tooba.Cart.Contracts` and `Tooba.Cart.Domain` have no entry, so their root surface is unguarded. |
| `forbiddenTopLevelFolders: []` everywhere | The manifest locks **no** top-level folder for any Cart project, so the `TECHNICAL_AXIS_FIRST` shape (F7) is not detected by the manifest. |
| `structureCertified: true` while the durable test guard is RED | The certificate asserts a state that `CartArchitectureGuardTests` contradicts at this HEAD (F13). |

`structureCertified: true` is **not** reopened as an ownership question — the five layers are correctly
owned. It is a **cohesion/consistency** problem: the manifest is not strong enough to hold the shape
W2 will establish, and the test guard that backs it is stale.

## 2. Durable test guard (`Tooba.Cart.Tests/Architecture/CartArchitectureGuardTests.cs`)

Assertions and their live verdicts at this HEAD:

| Assertion | Verdict |
| --- | --- |
| `Cart_golden_boundaries_and_physical_layout_remain_clean` | **RED** — `AllowedContractsFolders = ["Checkout","Presentation"]` missing `"Lifetime"` (F13) |
| `AssertNoRootDump("Tooba.Cart.Contracts", ...)` | fails for the same reason (line 296) |
| `AssertNoRootDump` for Domain / Application / Infrastructure / Endpoints | GREEN |
| `AssertNamespacesAlign` for all 5 projects | GREEN |
| cross-module boundary assertions | GREEN |
| CQRS / endpoint-ownership assertions | GREEN |

Reproduced at HEAD: **22 passed / 1 failed / 0 skipped**.

The failure is **guard staleness**, proven by the fact that the file the guard rejects
(`Contracts/Lifetime/ICartPersistenceHoursSource.cs`) has a namespace that already passes
`AssertNamespacesAlign` (`Tooba.Cart.Contracts.Lifetime`). The folder is legitimate; the array was
never updated when the folder was added.

## 3. Allowlist arrays at HEAD (authoritative snapshot for W2)

```csharp
AllowedDomainFolders         = ["Aggregates", "Entities", "ValueObjects", "Events"]
AllowedApplicationFolders    = ["Ports", "Lifetime", "Conversion", "Commands", "Queries",
                                "Models", "Errors", "Presentation", "Validation"]
AllowedContractsFolders      = ["Checkout", "Presentation"]              // MISSING "Lifetime"
AllowedInfrastructureFolders = ["Persistence", "Directories", "Messaging",
                                "DependencyInjection", "Events", "Security",
                                "Migrations", "Lifetime"]
AllowedEndpointsFolders      = ["Storefront", "Errors", "Resources"]
```

`AllowedApplicationFolders` currently **legitimizes** the `TECHNICAL_AXIS_FIRST` shape (`Commands`,
`Queries` at the root) and the `Models` tombstone folder. W2 must rewrite this array to the
capability-first shape and remove `Models` — otherwise the durable guard would reject the correct
structure and accept the wrong one.

## 4. Size baseline (`Tooba.Host.Tests/Baselines/tmar-source-size-baseline.json`)

`CartDirectory.cs` (831 LOC) is **absent** from the baseline, therefore the guard reports it as a
`NEW_OVERSIZED_FILE`. Two lawful resolutions: decompose below 800 (W1's plan, F5) or record it in
the baseline. The AMSC run chooses **decompose**, because the file genuinely carries three separable
concerns (F5) and recording it in the baseline would freeze debt rather than repair it.

No other Cart file is at or near the 800 `thresholdNewFileLoc` ceiling:

| File | LOC |
| --- | --- |
| `Infrastructure/Directories/CartDirectory.cs` | **831** |
| `Application/Errors/CartExceptionMapper.cs` | 289 |
| `Endpoints/Storefront/CartStorefrontEndpoints.cs` | 251 |
| `Domain/Aggregates/ShoppingCart.cs` | 238 |
| `Application/Presentation/CartPresentationComposer.cs` | 214 |

## 5. SoT (`docs/architecture/tmar-current-state.json`)

Current Cart-relevant SoT blocks:

- `completeReferenceModules[module=Cart]` — `state: COMPLETE_REFERENCE_PATTERN`,
  `lastAcceptedTask: TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001`,
  `lastAcceptedCommit: 6e880942`, `lastPostCertificationRepairTask: ...-R1`, commit `eea83df9`.
- `hostCartBoundary` — Host Cart authority ZERO, module route ownership, expiry/persistence policy
  Cart-owned.
- `cartMultiCurrency` — line-level currency authority (platform task, unaffected).
- `structureCertification.certifiedModules` — `Cart` present.

**No SoT block records the AMSC run.** W3 must add a `cartAmsc001` block (or equivalent) recording
the W0–W3 lineage, the final commit, and the certification verdict, without contradicting the
existing `COMPLETE_REFERENCE_PATTERN` state.

## 6. Manifest obligations carried into W2/W3

1. Add `Tooba.Cart.Contracts` and `Tooba.Cart.Domain` project entries (root allowlists empty).
2. Set `forbiddenTopLevelFolders` for `Tooba.Cart.Application` to
   `["Commands", "Queries", "Models"]` after the capability-first move, so the old technical-axis
   shape cannot return silently.
3. Repair `AllowedContractsFolders` (add `"Lifetime"`) and rewrite `AllowedApplicationFolders` to the
   capability-first set in `CartArchitectureGuardTests`.
4. Add a durable assertion that `Application/Models/` does not exist (tombstone deletion is
   permanent).
5. Add a durable assertion that `CartDirectory.cs` stays under the 800 LOC ceiling.
6. Record the final commit and certification verdict in `tmar-current-state.json`.

## 7. Host final-closure interaction

`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` / `HOST_ROOT_FINAL_CERTIFIED` are present in SoT.
The Cart manifest entry does not authorize any Host production file. This wave and W1–W3 add or
modify **zero** Host production files. `HOST_FINAL_CLOSURE_REGRESSION = NONE`.
