# TB-SHIPPING-MVP-OPERATIONAL-VERIFY-058 — evidence index

| File | Contents |
| --- | --- |
| `log-target-identity-and-db-inventory.txt` | Proven local Development target, running process identity, READ-ONLY Geography/Shipping/Fulfillment inventory, before/after integrity check of `tooba_alpha` |
| `log-runtime-attempts.txt` | Discriminating probe of the running process (route absence proof), isolated disposable clone + real HTTP matrix on the new code, root cause of the first failed startups, cleanup and integrity proof |
| `log-source-verification.txt` | Endpoint registration, route tables + permissions, authorization fail-closed chain, price-authority OFF proof, CQRS/DI composition, build/test evidence |
| `probe/host058d-http-probe.txt` | Raw real-HTTP matrix against the isolated new-code instance (port 5099) |
| `probe/host058d-stdout.txt`, `probe/host058d-stderr.txt` | Isolated instance startup log (successful run) |
| `probe/host058-stdout.txt`, `probe/host058-stderr.txt`, `probe/host058b-*`, `probe/host058c-*` | Earlier failed attempts (stale-env root cause) retained as evidence |
| `probe/host058-pid.txt` | Identity of the first isolated launch attempt |

Verdict: **PARTIAL** — real HTTP proof of route registration + fail-closed authorization on the new code;
authenticated admin CRUD and frontend round-trip BLOCKED; zero writes to `tooba_alpha`; release verdict
`NOT_READY_FOR_CHECKOUT_CUTOVER`.
