# TB-279-R2 — KEY CONTAINMENT

`Security-Exposure-State = CONTAINED_PENDING_REVIEWED_RETIREMENT`

This document never prints, copies or hashes-in-place any key VALUE. Only file identity, size, timestamps and truncated
SHA256 digests are recorded.

## 1. What was exposed in Git

The 279-R1 change accidentally added a plaintext ASP.NET Core Data Protection key ring file:

```
src/backend/Host/Tooba.Host/App_Data/dataprotection-keys/key-6db9ecd9-…-8a8d839da3d5.xml   1015 B
```

together with 21 historical task scratch scripts under `scripts/` (tb266r1, tb270r2, tb276, tb277r1, tb278, tb279) and
one historical `RESULT.bridge.txt` under the 277-R1 evidence folder. No key value, connection string or token has been
written to any evidence file, commit message or Bridge payload.

## 2. Preservation before untracking

Every incident path was copied to a private local directory outside the repository **before** the Git index was touched:

```
D:\temp\tb279r2-containment\
  preservation-manifest.txt
  files\<flattened-path>            (23 files)
```

The copy was verified by size + SHA256 comparison against the originals (first 16 hex chars recorded below). The
originals were **not deleted** — untracking is a Git-index operation only.

| Preserved file | Bytes | SHA256[0..16] |
| --- | --- | --- |
| `…dataprotection-keys__key-6db9ecd9-…-8a8d839da3d5.xml` | 1015 | `668887095BB63745` |
| `…277-R1__RESULT.bridge.txt` | 8908 | `D6C7C90338EA0EDD` |
| `scripts__tb266r1-admin.mjs` | 1604 | `24D05664D1F2C532` |
| `scripts__tb266r1-finish.mjs` | 3081 | `4F5D4BF85FDFBEEA` |
| `scripts__tb266r1-groups.mjs` | 2097 | `15ADB9DB6F7B5B36` |
| `scripts__tb266r1-probe.mjs` | 3256 | `323858E4B1501A15` |
| `scripts__tb266r1-probe2.mjs` | 3253 | `E86DB05AFA52CACD` |
| `scripts__tb266r1-probe3.mjs` | 4125 | `6BD22379F2FF2534` |
| `scripts__tb266r1-probe4.mjs` | 2683 | `2FAF41F912E80025` |
| `scripts__tb266r1-seed.mjs` | 9648 | `E0120B04F550CB71` |
| `scripts__tb270r2-dump-task.mjs` | 653 | `B4F843A3DC4F5097` |
| `scripts__tb270r2-persist-task.mjs` | 618 | `C9C5F1288B641451` |
| `scripts__tb270r2-probe-bridge.mjs` | 859 | `932964335067782C` |
| `scripts__tb270r2-probe-bridge2.mjs` | 376 | `A6397E57D0B34818` |
| `scripts__tb270r2-probe-row.mjs` | 394 | `6327B47B6C9A8A68` |
| `scripts__tb270r2-probe-swagger.mjs` | 508 | `7BC76788127BE42F` |
| `scripts__tb270r2-read-bridge-db.mjs` | 956 | `69D75A6741026711` |
| `scripts__tb276-read-bridge-db.mjs` | 953 | `0BC3A9C8E2317DFC` |
| `scripts__tb277r1-post-result.mjs` | 1724 | `58D874ADF9CF6981` |
| `scripts__tb278-post-result.mjs` | 868 | `39C03321491A45FD` |
| `scripts__tb279-post-result.mjs` | 924 | `6D617A75BCDC1638` |
| `scripts__tb279-read-bridge-db.mjs` | 970 | `E9246BD5EA26ACE1` |

## 3. Git containment (commit `22a0ec148`)

- Index removals only (`git rm --cached` on the explicit paths), no working-tree deletion.
- Exact scoped rules: `**/App_Data/dataprotection-keys/` and the specific script/report paths; no blanket `scripts/`
  ignore and no production file excluded.
- Verification: `git check-ignore -v` resolves the key XML to `.gitignore:23:**/App_Data/dataprotection-keys/`;
  `git status --porcelain` was empty after the commit; `git ls-files` shows no key or incident script.
- Force-push/history rewrite was **not** used and is not authorized by this task.

## 4. Is the exposed key active?

- The ring is the Shipping module's Data Protection ring:
  `ShippingModule.cs` → `AddDataProtection().SetApplicationName("Tooba").PersistKeysToFileSystem(<contentRoot>/App_Data/dataprotection-keys)`.
  It protects carrier account secrets/tokens (`ShippingCarrierSecretStore`, Tipax token sources).
- The source content-root ring (`src/backend/Host/Tooba.Host/App_Data/…`) contains exactly ONE key — the exposed one
  (`key-6db9ecd9-…`, 1015 B, written 2026-11-17 18:53 local). Anything the Shipping module protected while the Host ran
  from that content root was protected with this key and can be decrypted with it.
- The SERVED Host runs from `artifacts/publish-primary` and persists a DIFFERENT key
  (`key-d82e31a6-…`), so served-instance cookies and ciphertext do not depend on the exposed source key.
- Therefore the exposure is real for the source-root environment only; it is not a claim that live traffic was
  compromised.

## 5. Retirement / replacement (needs explicit approval — NOT executed here)

Data Protection rings are additive: deleting a key from the ring does not decrypt existing payloads with a new key.
The minimal safe sequence is a reviewed operation, not a hot edit:

1. Confirm which persisted payloads in the source-root environment were protected by `key-6db9ecd9-…` (carrier secret
   store rows and any auth cookies issued while that Host ran).
2. Enable a protected key ring (certificate/KeyVault/DPAPI) or replace the plaintext ring with a filesystem ring that is
   not inside the repository tree, then restart the Host so a NEW key is generated and made the default.
3. Re-enter/re-encrypt the affected carrier secrets through the owning admin surface so their ciphertext moves to the
   new key; expire dev cookies.
4. Only then remove `key-6db9ecd9-…` from the ring, accepting that anything still encrypted with it becomes unreadable.
5. Treat the historical Git blob as permanently exposed; no history purge is proposed.

## 6. Residual risk

- The key material remains in Git history and in the private preservation folder; containment prevents further
  propagation, not decryption by someone who already has the blob.
- Carrier secrets/tokens that were written in the source-root environment are the only plaintext-equivalent assets at
  risk; they must be rotated by the owning integration owner as part of step 3.
- No session, cookie or encryption setting was changed by this task, so no user or served behaviour was interrupted.
