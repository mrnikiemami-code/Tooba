# RECEIPTS — TB-OCC-284

## Identity

- Repository: `mrnikiemami-code/Tooba`, `main`.
- Expected base: `f006fbdbdbfd4911ea439fe9a3443438af6cf786` (ancestor).
- Execution base: `844c23237d19aeabc453f5fb04561657c4dd967a`; `HEAD == origin/main` before this task's commit.
- Source evidence commit: `fc40d412aa6d085d3db3f1deac461eb93a10f48d`.
- Reviewed product code: `611f97a85ec7c2e464e4f649820992202ca3754c`; no `src/backend` or `src/frontend` diff after it.
- Legacy Cursor paths: no post-844 diff. The original plan/review gate logs remain unchanged.

## Command and result

```powershell
node docs/opencode/tasks/TB-OCC-284-OPEN-CODE-ACCEPTANCE-CLOSURE/VERIFY-OPEN-CODE.mjs
```

Actual result: exit 0, `ok: true`, `selfTest: PASS`, 7 requirements, 38 connected assertions, 7 artifacts. The full stdout is `verification.log`.

Self-test performed inside the checker:

- baseline valid manifest/evidence: accepted;
- delete REQ-07: rejected;
- change REQ-01 expected price: rejected;
- tamper connected evidence/hash: rejected.

## Source and tool reasons

- OpenSpec 2.1.1 strict: `Change 'seller-offer-safe-edit-283' is valid`, `EXIT=0`; log SHA-256 `84a220d56bbb1540b9f95c898932fe9b6f675b55480da9b1fbc36d6e1f62f77e`.
- Playwright 1.65.1; Chromium path `C:\Users\User\AppData\Local\ms-playwright\chromium-1255\chrome-win64\chrome.exe`; tool file SHA-256 `5915408c216027d1929d56137c629767c6b0db4a0266cb70102283810febadcf`.
- Connected JSON SHA-256 `0a155bcc2fa5396b8d981e3246036523f70800a3bdf2e02db5c08fb89528afc1`.
- Harness SHA-256 `ebf1f993594684a08bed26d8be9aab87420d2522465cf3f4434597f5a535dc36`.
- Connected run log SHA-256 `29454955b7d09a3999d18a0bae42a4c4ba35b38c82eb66c887e32cfb595acf3a`.
- The source evidence hashes were recomputed from `fc40` Git object bytes for the 284 evidence files and from the working tree for the OpenSpec/Playwright evidence.

## Cleanup check

- Read-only current check: `tooba_offer283_connected` returned no row.
- Ports 5090 and 3100 had no listener.
- Canonical Host 5088 `/health` returned 200.
- Canonical Next 3000 `/fa` returned 200.

## Independent conclusion

OpenCode acceptance: **PASS**. The Cursor gate remains **KNOWN_FORMAT_INCOMPATIBILITY** and is not relabelled. No candidate method is canonical. No product source, schema, migration, live data, config, Cursor file, roadmap or Bridge file was changed by this task.

STOP_FOR_ARCHITECT.
