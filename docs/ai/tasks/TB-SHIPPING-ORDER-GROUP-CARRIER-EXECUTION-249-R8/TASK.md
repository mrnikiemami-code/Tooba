PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R8
Parent-Task: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R7
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — close ambiguous create outcomes and recovery UI
Mode: CORRECT_OUTCOME_CLASSIFICATION_AND_PERSIST_IN_FLIGHT_UNCERTAINTY
Expected-Base-SHA: 480241eff644b300443a40f0a712085ec932cb2a
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

BOUNDED OBJECTIVE
Close the remaining R7 response-loss state defects in the existing shared component. Preserve the implemented occurrence identity and backend provider-claim protocol. No new carrier feature or UI redesign. Focus on one coherent state flow; do not expand into unrelated shipping work.

SOURCE-CONFIRMED DEFECTS AT BASE

post() maps every non-OK response except one explicit unknown code to outcome refused. HTTP500/502/504 may follow a committed side effect and are not proof of refusal. Conversely, HTTP200 with decodeShipment(payload)==null returns outcome succeeded. Both cases clear uncertainty without authoritative evidence.
The first create persists occurrence with uncertain:false BEFORE await. uncertain:true is persisted only after catch/unknown. Reload/unmount while the request is outstanding loses the unresolved marker; refresh may clear its locator when success+remaining quantity allows another create.
Reconciliation UI is nested under group.canCreate. Real Pending/Unknown normally has canCreate=false, hiding this recovery path. loadError also renders only an error with no explicit read retry. The reconcile handler clears uncertainty on listOk alone rather than resolving this exact occurrence. Valid JSON/identity and exact-operation state matter; malformed successful lists must not become verified empty state.

REQUIRED CHANGE

Persist an in-flight/unresolved occurrence BEFORE POST can leave the browser; retain through remount and missing response. Only authoritative exact-occurrence outcome or a known pre-side-effect typed refusal can resolve it.
Classify network exceptions, generic 5xx, malformed/unexpected successful body, mismatched occurrence/group identity and unknown codes conservatively. No success from invalid DTO; no definitive refusal from generic server failure.
Validate authoritative read payloads. Retain exact occurrence and block minting a replacement when read is failed/invalid/uncertain.
Make explicit read/reconcile available independently of canCreate, including Pending/Unknown and read-error states. Do not enable provider Create as a substitute for tracking/reconciliation.
Resolve by EXACT occurrence identity, not just a successful list request. If no matching row is observed, preserve same locator and use only a documented safe same-occurrence recovery path; absence of a row is not permission for a new occurrence.
Keep deliberately new identical parcels distinct after confirmed state and lawful remaining quantity. Preserve no-resend Unknown, owner validation and existing physical specification authority.
Scope persisted hints to authenticated owner/context as required by R7; never store credentials or treat hints as authorization. If current owner cannot be obtained safely, fail closed rather than inventing identity.

TARGETED EXECUTABLE PROOF
Mount actual shared component with realistic capability states:
A. First POST outstanding -> unmount/remount -> authoritative success of same occurrence: no replacement identity/provider call.
B. HTTP500/502 after simulated accepted create, network loss, HTTP200 malformed JSON/DTO and mismatched returned occurrence remain unresolved; safe recovery retains original id.
C. Pending/Unknown with canCreate=false exposes appropriate read/reconcile controls, zero create calls. Read500 or malformed200 stays blocked and can be explicitly retried.
D. Known typed pre-side-effect validation refusal remains correct and editable. Exact successful occurrence resolution permits an explicit separate new parcel if remaining quantity exists.
Use existing controlled backend/relational tests only where change requires it; no new disposable matrix just to replay already-proven backend behavior. Retain adjacent R6/R7 assertions. Typecheck/build canonical deployed code.

PRESERVATION / FINAL DELIVERY
Same user's canonical D:\Users\User\source\repos\SarvNewVer FE3000 -> Host5088, SingleStore/store-alpha/tooba_alpha. No new final ports/DB, screenshots, provider credentials or real Tipax calls. No real user order/payment/fulfillment mutation. Preserve DataProtection and user data. No migration expected for this component repair; justify any new schema requirement before expanding.
Validate actual base; preserve intervening changes; no force/reset/stash/rebase.
Deploy after focused proof. Update ALL recovery authorities:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Record exact software acceptance and external Tipax verification deferred, not blanket production acceptance. Distinguish receipt commit SHA from final envelope SHA.
Preserve full TASK in docs/ai/tasks/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R8/TASK.md and evidence TASK/RESULT/RECEIPTS.
Return measured tests and canonical runtime parity. STOP_FOR_ARCHITECT; no automatic next task.
END_TOOBA_TASK
