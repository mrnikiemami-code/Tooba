# Manifest structure

File: `docs/architecture/tmar-module-structure-manifests.json` (schema `ARCH-COMPLETE-002`).

Before W2 the `modules` array had 32 entries and **no `Shipping` entry**. W2 added one
repository-consistent entry (now 33 entries, Wallet's entry unchanged, no other module touched):

```json
{
  "module": "Shipping",
  "structureCertified": false,
  "lockVersion": "ARCH-COMPLETE-002",
  "certificationNote": "NON-CERTIFIED physical-structure entry recorded by ... TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2 ...",
  "projects": [ Contracts, Domain, Application, Infrastructure, Endpoints ],
  "structureAuthorityTask": "TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2",
  "structureHandoffState": "READY_FOR_CERTIFY_PENDING_ARCHITECT_REVIEW",
  "certificationState": "NOT_CERTIFIED",
  "evidence": "docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2/RESULT.md"
}
```

Honesty rules applied:

* `structureCertified` is **false** — no whole-module certification, no `COMPLETE_REFERENCE_PATTERN`
  verdict, no manifest flip. Certify remains the only owner of that verdict.
* The declared project list is the five existing production projects, each verified to have a real
  `.csproj` on disk.
* `rootAllowlist` / `forbiddenRootFiles` / `forbiddenTopLevelFolders` describe the real post-W2
  physical roots (see `root-allowlist.md`); nothing was widened to hide debt.
* No schema was invented: keys used are the ones already present on other module entries.
* No `structureAuthorityCommit` field was written, because a self-referencing SHA cannot be honest
  inside the same wave; the commit SHA is recorded in `RESULT.md` instead.
* W0/W1 evidence and all other module entries were left untouched (no reserialization of the file;
  a single textual insertion was made before the `modules` array terminator).

`tmar-current-state.json`: no Shipping structure-handoff field exists in that document for this
surface, and adding one is not required by the current process. No edit was made there; the shipping
handoff state lives in this manifest entry plus this evidence folder.

Durable guard: `ShippingModuleStructureGuardTests.Manifest_entry_is_present_and_explicitly_non_certified`
(entry exists, `structureCertified == false`, declared projects == on-disk projects).
