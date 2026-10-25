PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214
Parent-Task: TB-SHIPPING-STOREFRONT-CUSTOMER-RECEIVING-CALENDAR-213
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: a6e0c6816b31482daac7158496686d17f3a2a92f
Mode: BOUNDED_SHIPPING_OWNED_LOSSLESS_REVISION_CAPTURE
Hard-Timebox:25min; stopnewimplementation17; final8 focusedproof/evidence/scopedcommit/push.
One correctionround max. HonestPARTIAL on limit; no newdiscovery/audit task. Userwantsfastdelivery.

DECISION / READ
TOP AGENTS/recovery,213Architectreview, rules§2/13/18/19/29, applicableAMSCbackend skill.
213calendar primitive accepted11facts; don't replay. Existingowner scopes already implement
Store/Marketplace/Seller authorization; use THEM, don't reject all nonStore.
212legacyflattenedpublish rejected. 212-R1 facts corrected; none of its livepointer proposal approved.
This task explicitly authorizes immutable REVISION CAPTURE as staging primitive, NOT publish/go-live.
No livepointer, activeflag, eligibility/readiness inference, publicpublishbutton/route or customerconsumer.

IMPLEMENT ONE COHESIVE PRIMITIVE
Inside Shipping only, create an immutable captured snapshot of one EXACT draft revision:

Extend existing authoring Application directory port with one typed CaptureRevisionAsync operation
and exact owner+draft+revision read. Existing adapter registration reused; no generic repository/framework.
Typed snapshot facts owned by Shipping. Complete lossless copy of ALL current persisted authored state:
trusted owneridentity, binding service/option, rootid/revision/created/updated, stored typeid (raw), ordered
groups/serverids/currency, every area identity+canonical storedancestry, every row identity+ALL bounds
(weight/count/merchandise) and freight+packaging+minmaxdeliverydays, ALL active/inactive/raw translations
and group titles (languageids/nullvsblank), weeklytimezone+7days/rememberedclosedhours, independent settings
and metadata. Enumerate sourceproperties once and compare snapshot readback against complete sourcedraft.
No catalog/geography/language read during capture: snapshot records already-ownedfacts, not newprovenance.
No renamingclocksemantics/defaultvalues/trim/rounding or storing only visible/active-language wireprojection.
Additive module-owned immutable persistence with formatversion. Architect permits ONE new table
shipping.authoring_revision_snapshots keyed by trustedowner+draftid+sourceRevision (or normalizedequivalent),
capturedtime and versioned typed JSONB payload to preserve this graph without duplicating rule tables.
Use internal explicit typed domain/persistencecodec, NOT raw HTTP body, unsafe typenames or arbitraryopaque
dictionary. ContractsDTO isn't persistence authority; no catch-to-empty decoder. Decimals/TimeOnly/nulls
must roundtrip exact domainvalues/fractions. No newpackage or foreignFK/crossschemajoin.
Additive generated migration+Designer/snapshot for ONLY this table/ownedindices/checks, no oldmigration
rewrite, no outbox/rate/policy table mutation, no schema-widebaseline refresh.
Capturing SAME trustedowner+draft+revision again is idempotent: return original capturedidentity/time/content,
one row, no secondwrite pretendingnewpublication. Changed currentdraft doesn't overwrite oldcapture.
Owner+draft not found -> existing safe notfound, wrongexpectedrevision -> existing canonicalconflict,
no capturedrow. No clienttenant/owner currency authority. Preserve ALL currently authorizedowner kinds.
Atomicity: captured root+children must be ONE consistent revision. Reuse module relationalpatterns or
explicit exact capturetransaction+rootlock/recheck. No race window from readgraph/recheck/INSERT.
No bumping draftRevision/UpdatedAt just to capture. No separatelycommittingnestedcommandhandlers.
If one explicit new transaction member needed, Architect approves ONLY that exact named member and actual
callers for this capture path; retain existing SaveAuthoringReplacementAsync/calleruniverse unchanged.
Update staticguard only narrowly for this exact boundary with negative case; no filename/generalallowlist.
No transaction work when same snapshot alreadyexists beyond boundedconsistentread.

READ EDGE
Add Shipping.Contracts AUTHORING REVISION reader only if needed for futurecrossmoduleconsumption;
exact capturedrevision lookup, serverboundowner scope, no 'currentactive' lookupbydestination, no default
latestfallback. FutureOrder composition must resolve trustedowner independently; neverbrowserowned.
No Order/FulfillmentforeignApplication reference, no newquoteengine or runtimeaggregateconversion.
If no externalconsumeryet, internal port+persisted snapshot is sufficient; don't create unused broadAPI.
No MediatRrequest/endpoint merely to make unusedinterface reachable; laterexplicitpublishuses this primitive.

FOCUSED PROOF (existingShippingTests)
Use current faithful owner fixture/directory/DbContext conventions; no newproject/package.
1 complete roundtrip with two rates in SAME area carrying DIFFERENT packaging, all bounds/currency,
deliveryrange, raw multilingual/null-vs-empty, type/weeksettings/rememberedhours.
2 mutate current draft aftercapture; original capturedpayload unchanged; newrevisioncapture different.
3 same revisioncapture twice yields one immutable row/identity/time.
4 foreignowner/stale revision rejected with no row, drafttracker clean/no laterSave leak.
5 concurrent edit-vs-capture consistent oldornew revision/conflict, neverhybrid; real relational proof
only if disposableprovider available, no EFInMemory pretendingtransaction/concurrency proof.
Build projectreferences +focusedtests once. No broadShipping626/645/Fulfillment/Host/solution/frontendloops.
Disposablelocal PostgreSQL only, parsedfailclosedtarget+liveprovenance, existinglocalimage, exactcleanup;
NO shared5432/tooba_alpha migration/datawrite/restart for214. If unavailable report relationalproofblocked,
do not silently substitute model-createdschema/providerclaim.
No testinstall or browser/server/merchantmutation. Prior explicitdev permission doesn'trequireaskingagain,
but this task bounds sharedruntimeunchanged untilreview.

DELIVERY / NEXT
Persist task and docs/architecture/evidence/TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214/RESULT.md,
actualmigrationdelta/fields/tests/counts/provider/atomicitylimits. No capture of realuser draft here.
TOP recovery+AGENTS: stagedcapture implementation vs NOTCUSTOMERACTIVE, exact next integration step,
remaining samegroupafterdiscountANDtax authority/measurement and publishselectionconsumerwiring.
Publicsave remains authoring-only; no 'Storefront done' claim. ExistingFE3000/Host5088/userunsaveddata
unchanged,TB185paused/defaultOFF/PendingAllocation intact. No task213calendar replay.
GitstageEXACTtaskpaths only, nevergitadd-A or historicalpostresultscript. Scopedcommit/push/fetchparity,
actualtime/corrections, STOP_FOR_ARCHITECT, Automatic-Next-Task NONE. No further executableenvelope.
