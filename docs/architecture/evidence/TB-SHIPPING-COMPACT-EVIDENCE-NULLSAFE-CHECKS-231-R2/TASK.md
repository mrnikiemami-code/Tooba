PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-COMPACT-EVIDENCE-NULLSAFE-CHECKS-231-R2
Parent-Task: TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231-R1
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Mode: PENDING_MIGRATION_CHECKS_AND_DIRECT_POSTGRES_FACTS_ONLY
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
HARD15min / source6 / provider6 / recovery3. One correction.
231-R1 VALUE/TRANSFER ACCEPTED; DO NOT redesign/reimplement it or re-run all50facts.

SCOPE EXACTLY
Existing unshared migration20261027150000_AddShippingGroupAcceptedEvidence.cs,
existing ShippingGroupAcceptedEvidenceStorageTests.cs, evidence and three TOP recovery files.
No Domain/DTO/mapping/column/table/index/transaction/amount/owner semantics change.
22scalar columns remain; noJSON, no cross-moduleFK. EF snapshot baseline gap retained, no regeneration.
No live composer/writer/parity, no shared migration, no Host/FE restart, no browser or user-review-window
enumeration/automation/navigation. Preserve all live data/previewON/count7/not_ready/TB185paused.

CONFIRMED FAILURES TO PROVE RED ON CURRENT CHECK
1 minNULL/max3 and min2/maxNULL pass PostgreSQL CHECK as UNKNOWN.
2 ownerKindStore with tenant"" or spaces passes ISNOTNULL despite domain nonblank.
3 all-zeroUUID required snapshot/draft/area/row or optional-presenttype/Sellerowner passes nonnull tests.
4 currency123 or A$! (three chars) passeslength3 despite domain ASCIIuppercase3letters.

FIX ONLY EXISTING CHECK EXPRESSIONS ON BOTH TABLES
- optional delivery metadata either BOTHNULL, or BOTH explicitly NOTNULL plus existing0..min..max..365.
  Make false/true result, not UNKNOWN, for half pairs.
- Store tenant nonblank/canonical under existing trim policy, maxlength already64; never lowercase/guess.
- required referenceUUIDs nonempty; optional-presentShippingTypeId nonempty; Sellerowner nonempty.
- currency exactly3ASCIIuppercaseletters (existing canonical currency semantics), no currency default.
Preserve complete absence ALLacceptedcolumnsNULL as lawful legacy; complete validStore/Marketplace/Seller
and optional min/max BOTHNULL remain lawful. Do not accidentally reject explicit zero amounts.
NoSQL precision-policy redesign: numeric(18,2) coerces beforeCHECK, so app exact-amount input validation stays
the authority; do not claim SQL preserves raw out-of-scale input.

PROVIDER PROOF ONLY, EXISTING HELPER/TARGET
Use ONE existing-image task-owned disposable PostgreSQL at15433/order_tb231/tb231; assert fixed identity
BEFOREDDL, no shared5432, no relaxing reviewed helper. DirectSQL updates must bypass constructor to exercise
actual DB constraints. Assert SQLSTATE23514 and expected constraint name, then intact row after each refusal.
Cover both cart_shipping_groups and order_shipping_groups, half day pairs, blank tenant, zero refs/type/owner,
bad currency; control BOTHNULLdays and validallownerforms/zero amounts and full legacyNULL clear remain valid.
RED first on unchanged migration proves counterexamples. Then normal migrator DOWN ONLY the231 migration
and UP corrected migration (or recreate the SAME task-owned empty DB) beforeGREEN: do not merely edit code
and reuse an already-applied history row; do not manually alter history/adopt old constraints.
One container only; task DB ephemeral. Remove own container/verifyportclosed.
Execute exact storage class + new direct cases only, no Domain11/Isolation12/materializer50/Host/solution
confidence loop. Normal test build as needed; report actualcounts/RED/GREENtruthfully.

FINISH
docs/architecture/evidence/TB-SHIPPING-COMPACT-EVIDENCE-NULLSAFE-CHECKS-231-R2/{TASK.md,RESULT.md}.
Append correction to231-R1RESULT preserving earlier proof history.
Update AGENTS +START-HERE-IF-CHATGPT-IS-LOST +canonical shipping recovery TOP:
value/transfer accepted, actual CHECKfix/proof, migrationstillWRITTEN/sharedNOTAPPLIED, no checkoutwriter.
Next after Architectreview: trusted server selected-revision/cart/Offer/type/quote/date revalidation writing
compactevidence then actualselection/commit; no fullJSONperorder/nohistoricalrepricing.
Known read-onlyUI/cross-ownerlabel/decoder/OfferAPI gaps retained; dedicated REVIEW window untouched.
Exactpathcommit/push/fetchparity, noadd-A/historyrewrite/Bridge/post-resultscript.
STOP_FOR_ARCHITECT. Automatic-Next-TaskNONE. No userVisualACCEPT claim.
