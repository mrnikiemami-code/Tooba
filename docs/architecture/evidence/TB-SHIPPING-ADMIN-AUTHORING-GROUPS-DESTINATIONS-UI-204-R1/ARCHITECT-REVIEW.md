# TB204-R1 Architect review — 2026-10-24

Disposition: REPAIR_REQUIRED for two remaining picker evidence/current-chain defects. Preserve the implemented sibling-request fix, proper clear functions, terminal-level ancestry and unavailable-label behavior.

Source independently inspected at0b1f9abcbad8c1a5fc244ef5354413510dcf1cd8. Current main909e43ad26298a253f6e9cb41712bc76571b10d0 is a descendant differing only by the committed post-result.js evidence artifact; no extra production change. No Bridge script executed by Architect.

## Confirmed remaining defects

1. verifiedAreas retains earlier language|country sets after context changes. Returning A->B->A launches a fresh area request AND immediately launches cities using the old A set. This violates the promised prerequisite on re-entry, and cities can become usable before fresh parent evidence succeeds.
Architect executed the UNCHANGED coordinator with injected deferred promises: firstA areas resolved; updateB; updateA before newA areas resolved ->3 area reads and2 city reads, both city calls forA. The second city read already exists although freshAResolved=false. Fresh context identity must invalidate/rebind that parent evidence; a cached set must not discharge a new active prerequisite.

2. choiceChainActive only validates an administrative area when the UI has an explicitly selected administrativeArea. The legitimate country-wide city path keeps that field null; a non-null city.parentId is therefore neither checked for active parent nor tied to current area evidence at Add. The coordinator stores only ids, including inactive ones. Independent unchanged-module execution of activeCountry + activeCity(parentId=inactiveAdmin) + no selected area returns true. The real option set could contain that inactive admin and the decoder accepts its id, so this is reachable without a forged response.
The component's canAdd/handler use only staged choice flags, not current coordinator loading/failure/membership. A language/context refresh can retain previously selected rows as current evidence. Gate Add using the CURRENT successfully verified terminal path and actual current parent rows, re-checking synchronously in the handler. Country-only selection should not depend on unrelated city failures.

The adapter is byte-unchanged byR1. It still accepts parentId=null before considering an explicit area filter, and absent country-wide known parent evidence falls through true. The report's strict filtered-read claim is therefore not exact. Apply the small fail-closed decoder predicate alongside the current-evidence fix, keeping null-admin cities lawful on a verified country-wide branch.

## Reused progress and evidence limits

Worker reports12 coordinator/chain +9 group facts,68 feature-root facts,tsc and7 guards. Root-glob versus nested suite wording is now corrected. Architect did not rerun those suites; performed the two direct module reproductions above. Mounted React/browser/visual/live remain NOTRUN. No new broad audit or backend re-proof is requested. Prior sibling ordering, countrywide fresh-first-read, clear and label changes remain intact.

Only next204-R2: bounded current-generation parent evidence and Add gating/decoder repair,20minutes. Do not redesign grouping/transport or start205. Production functional acceptance remains PENDING.
