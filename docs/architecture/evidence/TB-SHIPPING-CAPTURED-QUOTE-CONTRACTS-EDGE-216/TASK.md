PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-CAPTURED-QUOTE-CONTRACTS-EDGE-216
Parent-Task: TB-SHIPPING-CAPTURED-MANUAL-QUOTE-215
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 88a28abf2eb1b9914c2e0872531e44dcd3f636be
Mode: BOUNDED_EXACT_CAPTURE_QUOTE_CONTRACTS_AND_GEOGRAPHY_ADAPTER
HARD25min,STOPimplementation17,final8focusedproof/evidence/commit/push. Onecorrectionmax.
No genericaudit/replayingprevioussuites/longloop. HonestPARTIALatlimit.
ReadTOPAGENTS/recovery,215Architectreview, rules§18/29; applicablebackend/modulecallskill.

GOAL
Make existing capturedMANUALquote safely callable through Shipping.Contracts using exact persisted
owner+draft+revision and REALverifieddestination. This is NOTactivepublication orOrderwiringyet.
Existing215engine remainsSINGLEpriceauthority. No lossycopy/anothercalculator/publishpointer.

THIN CONTRACTS EDGE
Shipping.Contracts: one cohesive typedrequest/result +IShippingCapturedRevisionQuoteLookup (exactnameflex).
Request: server-resolvedfullownerkey, exactdraftid/sourceRevision, DestinationIdentifiers,
groupWeightGrams?, groupItemCount?, FinalGroupGoodsAmountAfterDiscountAndTax?, currency.
No capturedpayload/clientquotedprice/ownercredential, no latest/current/activefallback.
Result: explicitoutcome/reason, nullmoneywhenNotQuoted, quotedcomponents+checkout/CODsplit and full
selectionprovenance/snapshotid/revision/group+rowids/rawtypeid/minANDmaxdays/receivingweek/timezone/settings.
No Domain/Application/Infrastructure types inContracts. MonetaryQuoted !=active/eligible; statusNotEvaluated.
No HTTProute/command/authbypass. Ownerkeyisdataidentity, NOTpermission; futureOrdercaller must deriveit
fromtrustedcart/fulfillmentresponsibility, notbrowser. Do not claim callermeasurementsoramountverifiedbyCart.

INFRASTRUCTURE ADAPTER
RegisteredONCEscoped inShippingModule. ReadONLYownShippingDbContext or existingexacthistoricaldirectoryport.
Validatecoherentowner+effectiveedition/commercecontext using existingfoundationseams:
Storetenantmustmatchcurrentboundstore;Marketplace/SelleronlyconsistentMarketplacecontext.
No fakeTenant/defaultStore/Seller inference, no blanketrejectallnonStore.
LookupEXACTowner+draft+revision first; missing/foreignsafeunavailable withZEROforeignGeo reads.
ThenReadPayloadheaderintegrity, buildexistingengineexpectedidentity, no draftread/recapture.
No contextwrite/SaveChanges, transaction, mutation of snapshot/clock.
Use real Geography.Contracts seams (existingIGeographyPlaceSnapshotReader/IGeographyLookup) for requested
terminal/parentclosure, existingShippingDestinationAncestry helper wherecompatible. No labels/language
lookup required for price; don'tcopygeographyvalidation.
Check activeactualcountry/adminchain/city/servicearea andexactparentidentities, unknown/inactive/cyclic/
contradictory=>notquotedDestinationUnresolved, nofakecoverage. Nulladmincity isLAWFUL.
Rejectduplicated/foreignidentities fromContracts response; no catch-to-empty providerfailures.
Canonicalmodulecalltracer peractualforeigncall with constantoperationtags/nopayloadids orlabels.
Propagatecallerexception/cancellationidentity, notemptypricefallback.
Oneengineinvocation, explicitDomain->Contractsmap, noJSONroundtrip/typecasts acrossmoduleboundary.

TWO NARROW DOMAIN CORRECTIONS AS PART OF EDGE
1 Current215quotes whenmatches==1 ANDundetermined>0. A potentiallymatchingalternativemissinganaxis canhide
ambiguity. ReturnMeasurementRequired/nullmoney until resolved, evenifoneknownmatch.
KnownOutside rowstillirrelevant (keep215control). Addexactcorruptpayloadregression+control.
2 CurrentDestinationScope hasonlyleafAdministrativeAreaId; rootadmincoverageofdeepchildcity won'tmatch.
UseverifiedFULLadminancestorclosure inengineinput/matching, additiveoptionalfield/overload/cohesivepathvalue
asneeded; adapter buildsitfromrealread. No pricingloop perancestor/firstsuccess.
Deepancestor+descendant differentgroups=>GroupAmbiguous, notdeepestwinner.
No trustfromrequest-suppliedancestors; ALLactualids mustbelongtoverifiedsamecountrychain.
ExistinglegacyQuote and current215publicconstructors/callers remaincompatible. No otherpolicychange.

FOCUSED PROOF (existingShippingTests; onebuild+focusedrun)

Exactcapturedread yieldsdeclaredrowmoney/provenance throughactualadapter +realDIregistration; sameinstance
perscope/differentscopes/descriptorcount. Controlledownstorage/Geographyproviderseams explicit, no fakeDI
registration of newadapter. RealEFInMemoryallowedforqueryonly ifexistingpackage; NOTtransactionproof.
Missing/foreignrevision +invalidedition/ownermismatch=>safeunavailable ZEROGeo reads, no rowwrites.
Deepadminparentmatchedfordescendantcity; wrongcountry/parent, inactive/cyclic/missing chain failclosed;
nulladmincitycontrol; duplicateoroutside-requestresponseidentityreject.
Domainunknownalternative fix+knownOutsidecontrol.
Exactmetadata/decimals/fractions; allNotQuotedmoneyNULL; no latestfallback.
OnecanonicalActivityListenerparent/correlation/control+sentinelexception/callertokenfact ifharnessalready
available; reuseexistingcompositiontestfoundation, no newframework. Reportlimitsifdeadline, don'tclaimspan
proof from 'tracerused'. Atleastactualcallertoken/exceptionidentity assertions required.
No43/18/8/119/fullShipping/Host/solution/provider/browser suite. No newpackage/project.

BOUNDS
No schema/migration/214capturechanges/Orderliveconsumer/FE/CSS/Host/configflag/activation.
No DBcontainer needed; don'ttouchshared5432 oruserdraft, noHostFErestart/seed/download/probe.
DefaultOFF/TB185paused/PendingAllocationintact. §29decisionsalreadyresolved, no newproductquestions.
This stagedContractsquote doesn'tchooseactivecapturerevision, realCarttax/discountgroupallocation orweight
authority yet. Explicitlystate NOTCONNECTED customerpath; no Storefrontcompleteclaim.
Existinglocaldevauthorizationpersists, no repeatedapproval.

DELIVERY
Taskverbatim +docs/architecture/evidence/TB-SHIPPING-CAPTURED-QUOTE-CONTRACTS-EDGE-216/RESULT.md,
exactfiles/cases/counts/DI+Geographyprovenance/limits.
TOPcanonicalrecovery+AGENTS+newchatguide: exactnexttrustedowner+revisionselection/groupfinancialauthority,
thenprojection/selection/commit, notanothergenericdiscovery. Preservehistory/alluserunsavedwork/runtime.
GitstageEXACTpathsONLY (nevergitadd-A/historicalpost-resultscripts). Scopedcommit/push/fetchparity,
actualelapsed+corrections. STOP_FOR_ARCHITECT;Automatic-Next-TaskNONE;no217envelope.