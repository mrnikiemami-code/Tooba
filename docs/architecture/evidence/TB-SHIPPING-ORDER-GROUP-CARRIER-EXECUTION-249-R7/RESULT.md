# RESULT — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R7

Status: **PASS** within issued prebind + mounted recovery + disposable relational + canonical deploy scope  
Decision: **OCCURRENCE_PREBOUND_BEFORE_CREATE_RESPONSE_LOSS_RECOVERY_DEPLOYED_PROVIDER_UNVERIFIED**  
NOT Architect ACCEPT, NOT live Tipax ACCEPT, NOT Visual ACCEPT

Parent: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R6`  
Base-SHA: `76e609065ddc3ac2bd64291117ceaa1a2644bf9a`  
Code-SHA: `78fe85d8cff9387e8e6b9b15196fe52071f59d1f`  
Evidence-SHA: `EVIDENCE_SHA_PENDING`  
Stop-State: `STOP_FOR_ARCHITECT`  
automaticNextImplementationTask: `NONE`

## Repair

Client UuidV7 occurrence minted and retained **before** first create POST (session hint scoped to checkout+group). Create body always includes `packageDispatchId` once prebound. Server binds supplied locator (R6 rules): same occurrence+payload → replay; conflict typed; new occurrence for explicit new parcel only. Uncertain create (lost/ambiguous response) blocks blind resubmit until explicit reconcile; remount/retry keeps same id. Failed list/read is not evidence of no operation. Legacy no-locator after Succeeded deliberately mints a new occurrence (documented unsafe for response-loss idempotency).

## Proof

- FE `test:carrier-mount` **24/24** (first create has stable id; lost-response retry/remount same id; uncertain block; explicit new parcel distinct).
- Directory prebound unit **1/1**.
- Disposable PG (container `tb249r7-pg`, hooks via TOOBA_TB249R6_POSTGRES identity on 15433): Category TB249R7 **3/3** — concurrent shared prebound Creates=1; legacy no-locator after success Creates=2 distinct; adjacent R6 barrier/identical/weight **4/4**. Cleaned; port closed.

## Canonical

No new additive migration. FE3000 PID **38896** → Host5088 PID **18428** SingleStore/store-alpha/tooba_alpha. DataProtection preserved. Authorized Manual+payment_locked. Tipax deferred.

## R6 evidence-SHA note (append-only)

R6 RECEIPTS stamped Evidence-SHA to interim docs commit `1e4bd4e13` then final envelope/push HEAD was `76e609065`. Treat **final pushed envelope SHA `76e609065`** as R6 Evidence-SHA for base/recovery; interim stamp remains historical in that file.

## Unmet / external-only

- Live Tipax deferred  
- Architect/Visual ACCEPT not claimed

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
