PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R1
Parent-Task: TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: a05ecc801b06d2195c32f7b131963afc33cc9b86
Mode: BOUNDED_CAPTURE_FRESH_GRAPH_PROVIDER_TRUTH_AND_RELATIONAL_PROOF
HARD20min, STOPIMPLEMENTATION12, final8proof/evidence/commit/push. Setup<=3min.
Onecorrectionround; no70minrepeat. AtstopreportPARTIALexactremaining, don'tstartnewwork.
No generic audit/fullguards/suite/confidenceloop.

READ
TOPAGENTS/recovery +214/ARCHITECT-REVIEW.md; applicablebackend skill for selectedscope.
Priorlocaldev authorization persists, no approvalquestion.
214domain28green is NOTDBproof. No214acceptanceyet. Userdecisions§29alreadyresolved, don'treask.
No Orderconsumer/publication/livepointer/legacyconversion; snapshotstagingONLY.

EXACT CODE FIXES
1 Persistedfreshgraph: FORUPDATE is currentlyAsNoTracking but next Scoped trackedquery may reuse stale
root/children. Load complete fresh UNTRACKED owner-scoped graph while SAMErootlockheld.
Capture must NEVERserialize or SaveChanges callerunsavedwork. Detectdirtytracker BEFOREanytransaction;
failclearly via existinginternalerror convention (an explicitInvalidOperationException is fine for this
internalAPI) leaving callertracker/valuesuntouched. Don't clear/detach userwork as 'fix'.
Existingcleanstaletracker may coexist; freshgraph usesDBtruth, not identitymap.
Sameowner/expectedrevisioncheck staysatomicwithsnapshotinsert; no draftrevision/time bump.
2 Afterinsert, return actualpersistedproviderfacts: JSONBnormalization/timestamptzprecision mean initial
inmemorystring/time differs fromfreshread. Reload snapshotinsidecapturetransaction beforefirstreturn.
Subsequentexactid/time/payload comparisons shouldcompareprovider-normalizedvalues, typedpayloadfidelity
stillmustpreserveeverydecimal/time/null/value. Don'tdeleteidempotencyassertions or onlycountrows.
3 Newcaptureuniquehandler mustmatch SQLSTATE23505+EXACT
ux_authoring_revision_snapshots_owner_revision (schema/tableifprovided). Don'treuseContains(tablefragment)
anddon'tchangeotherwritersglobally. Exactnamewrongstate/lookalike/PK propagate sameinjectedexception.
4 Add exactinternalGetCapturedRevisionAsync(owner,draftId,sourceRevision) to existingport/adapter:
readoldcaptureaftercurrentdraftedited, no latestfallback/recapture, safeownerfilterednotfound.
No newcrossmoduleContractsinterface/publicendpointneeded yet.
5 Codec: syntacticallyvalidJSONmissingrequiredidentity shouldn'tdefaulttovalid. Validate requiredshape,
ownercoherence/nonemptydraftid/positiverevision/binding/requiredcollections/nestedrequiredmembers/version;
ReadPayload mustreject payloadowner/id/revision/format inconsistentwithrowheaders.
Do notnewpricingvalidatecapturedincompleteness (emptydraft is stillvalidincompletecapture), no trimming,
clamping/fabricateddefaults. Preserveall3ownerforms/currentpersistedfacts.

STORAGE FACT REPAIR (not weakening)
ExistingtestcreatesTWOgroups then Groups.Single(); retainBOTH and selectintendedgroupbyactualserverid.
Fixturemustcoverfulltypedroundtrip, not onlyrates. KeepJSONBproviderstringcomparison honest.
Requiredfocusedrelationalfacts:

fullgraphcapture, samecapturetwice(originalpersistedid/time/content), historicalreaderafterlateredit,
foreign/stalereject noadditionalrow/laterSaveleak;
preloadOLDroot/childrenincontextA, contextB commits revision+newchildren, A capturewithBtoken => DBfresh
wholeBgraph (neverhybrid); Aoldtoken=>canonicalconflict;
callerdirtyentry => capturedoesn'tflushorclearit/no snapshot/noDBmutation;
twoactualcontextsduplicatecapture => onestoredrow/samewinner;
boundedcapture-vs-edit coordination => completeold/newcaptureorcanonicalconflict; nothybrid.
Explicitbarriertimeout<=10s, commands<=90s. No repeatedraceconfidence.
No EFInMemory presented as lock/transaction proof. Domain/codec+exactconstraintnegatives focusedonly.

ISOLATED PROVIDER — AUTHORIZED REPLACEMENT FOR BLOCKED55432
ExistingShippingRelationalTestTarget constants/negatives stayUNCHANGED.
ForTHISstorageclassonly addtest-owned exacttarget:
127.0.0.1:15432, database shipping_tb214, role tb214, env TOOBA_TB214_POSTGRES.
ParsedNpgsqlConnectionStringBuilder validation BEFOREconnection; reject multi-host/localhost/shared5432/
wrongDB/wrongrole; don'tderiveexpectedidentityfromconnectionstring. Assertcurrent_database/current_user/
inet_server_port BEFOREDDL. Taskownedpostgres:17already-presentlocalimage, no download.
Check15432free thenonecreateattempt. Ifbindblockedstopsetup<=3min; don'tnetshdeleteexclusions or
restartDocker/sharedservices, don'tpointshared5432orDBtooba_alpha. ReportBLOCKEDhonestly.
ApplyREALShippingmigrationchain onlytaskownedDB, verifynewtable/uniqueNULLSNOTDISTINCT andfreshread.
Removeonlyexacttaskcontainerafterproof, port15432silent. No broadcleanup/unrelatedfiletouch.
NOsharedDBmigration, userdraftcapture/write,Host/FErestart/browser,packages/downloads.

GUARDS/VALIDATION
Preserveexistingexact2transactionmembers andoriginalSavecallers. Newreadno transaction.
Oneprojectbuildifneeded, focusednewstorage+codec/constraintfacts and narrowlyrelevantexistingguard.
Do NOTrepeat90fact Guard|Compatibility|Structure|Snapshot wildcard; no626/645/solution/Hostsuite.
Actualfactcounts/pass/fail/skipped andfailedrunslogged, no missingproviderPASS.
Ifnewgenuinedefectoutofscope/time, stateitandstop.

DELIVERY
Taskverbatim +docs/architecture/evidence/TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R1/RESULT.md.
Append214correction: notmerelyportblock; stalegraph/dirtytracker/providerfirstreturn/testSinglegap found.
ExplicitlyREJECT worker214next 'mapontoexistingaggregates+UnrepresentableAxes': partiallossypublication
stillforbidden. Noarchitectureflip/gateON/TB185resume/defaultOFF/PendingAllocationchange.
TOPrecovery+AGENTS accurate nextstate, preservehistory/realuserrevision/unsavedtab/Host5088/FE3000.
GitstageEXACTpathsONLY; no gitadd-A/historicalpost-result execution. Scopedcommit/push/fetchparity,
actualelapsed. STOP_FOR_ARCHITECT; Automatic-Next-TaskNONE; no envelopefor215.
