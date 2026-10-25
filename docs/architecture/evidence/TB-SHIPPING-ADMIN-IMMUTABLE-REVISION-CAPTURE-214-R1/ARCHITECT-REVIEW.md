# Architect review — TB214-R1

Verdict: ACCEPT_PARTIAL_REPAIRS / HOLD_FOR214-R2_FAILURE_CLEANUP_AND_ACTUAL_CONTEXT_PROOF.
Independently reviewed371f850a20be98c265f187658c52f8990d4d0e05 diff, full storagefactsource andRESULT; rootdidnotrerun.NET/Postgres. FreshAsNoTrackinggraphunderrootlock, dirtyentryrefusalbeforetransaction, persistedJSONB/timefirstreturn, exactconstraintmatcher, historicalreader andcodecshapechanges are observed. Realprovider18/18 and43/43 are workerexecutionevidence, not proof of unexecuted branches below.

Definite remaining source issues:
- rollback(false) preservescallerstate but ALSO leaves the capture's own attempted snapshotAdded after a pre-save failure. LaterSaveChanges canpublish that failedcapture. Detach ONLY methodowned attempted snapshot on every failed/cancelled/unique branch, never Clearcallertracker.
- exactunique catch rollsbackthen sets transaction=null withoutdisposingoriginal; readswinner while completed EFCurrentTransaction mayremain. Mustdisposeowntransaction before recoveryread and never leakit; no cancellation-swallowing/unbounded None retry.

Actual tests do NOT prove two headline claims:
- Tracked_stale... createsstaleA but successful B-tokencapture runs through NEWfreshInA. ReplacefreshInA withACTUALstaleA; assertoldtrackedinstance remainsandreturnedpayloadisfullB. This is not source-pattern proof.
- Two_real_contexts... passesasync methods toWhenAll, but each first performs synchronousBarrier.SignalAndWait BEFOREfirstawait. Firstcanblock/timeoutratherthanrendezvous; returnedbool ignored. Use trulyconcurrent Task.Run withboundedgate andASSERTsuccess/twoarrivals. No confidenceloop.

Need only sameexactmembercleanup+thesefacts+failure/cancellation/unique-recoveryrealcatchfacts on existing15432isolatedtarget. No more schema/domainAPI/publishwork. Prior newtable stillnotsharedapplied.214R1actual48>20noncompliant, no processPASS despitebehavioralnumbers. Stopatnew12mincap, notanother48.

Remaining architecture continueslosslesscapture, no legacyflattening. Calendar213/FE119/ownerdecision§29notreopened. This isn't approval Storefront live; afterR2review nextactualruntimeconsumerwork, not broadaudit. No userdata/Host5088/FE3000mutations.
