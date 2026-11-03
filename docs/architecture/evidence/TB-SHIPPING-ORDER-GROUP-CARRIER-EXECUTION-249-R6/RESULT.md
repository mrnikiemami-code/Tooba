# RESULT — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R6

Status: **PASS** within issued A/B/C + disposable relational + canonical deploy scope  
Decision: **OCCURRENCE_IDENTITY_WEIGHT_AUTHORITY_AND_MOUNTED_COMPONENT_PROOF_DEPLOYED_PROVIDER_UNVERIFIED**  
NOT Architect ACCEPT, NOT live Tipax ACCEPT, NOT Visual ACCEPT

Parent: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R5`  
Base-SHA: `481ec58bb91ea85b787236119241c3fbb01e8e89`  
Code-SHA: `4150f0414bb732ae3d5e7dbf96876f1f602df258`  
Evidence-SHA: `EVIDENCE_SHA_PENDING`  
Stop-State: `STOP_FOR_ARCHITECT`  
automaticNextImplementationTask: `NONE`

## A — Distinct identical parcels vs replay

Occurrence = server `PackageDispatchId` (UuidV7), distinct from payload fingerprint.  
`operation_key=c:{checkout:N}:{group:N}:{occurrence:N}`. Same occurrence + same payload → replay (Pending/Unknown no-resend + exclusive claim preserved). Same occurrence + different payload → intent_conflict. New occurrence + identical payload → new allocation/provider call. FE retains occurrence across double-click/retry/Pending refresh; new occurrence only after Succeeded when canCreate.

## B — Typed weight never heals via legacy

Typed WeightGrams write requires whole grams. Resolve: legacy only when typed is null; present non-whole typed → null effective (never legacy 500 when typed 500.5). cm decimals retained; nullable unspecified; no fabricate/backfill.

## C — Mounted React proof

jsdom mount of shared `OrderGroupCarrierExecution` + product physical fields: package→sender→create (create=1 on double-click), Pending occurrence replay, Unknown, intent_conflict; physical g/cm labels, save/read, null clear, fractional refusal. `npm run test:carrier-mount` **18/18**.

## Disposable PostgreSQL TB249R6

`tb249r6-pg` `127.0.0.1:15433` shipping_tb249r6/tb249r6. Category TB249R6 **23/23**: identical A/B parcels Creates=2 qty2 money conserved, third exhausted; barrier same-occurrence Creates=1; conflict/cross-owner/no-resend; weight 500.5+legacy500 → null. Cleaned; port closed.

## Canonical

No new additive migration vs R5. FE3000 PID **13080** → Host5088 PID **45600** SingleStore/store-alpha/tooba_alpha. DataProtection preserved. Authorized Manual+payment_locked. Tipax deferred.

## Unmet / external-only

- Live Tipax account/provider acceptance deferred  
- Architect/Visual ACCEPT not claimed  
- Full product-workspace shell mount not required (shared physical fields fixture used)

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
