# TB-TMAR-NOTIFICATION-AMSC-001-W3-R3.task.md — Persisted claim artifact

Mode: RECERTIFY_AFTER_STRUCTURE_REPAIR
Skill: tooba-architecture-certify
Title: Re-certify Notification after authoritative shallow structure repair

Persisted claim artifact — full Architect body received via Bridge `GET /api/tasks/next?channelId=tooba-main` at claim row `677a74aa-7dbd-4260-9d3d-2bbbae509ac3` (createdAtUtc 2026-10-06T16:52:59.596574Z).

- Starting HEAD (required): `028ef769c4face48308a9dffb7f9714826e1be7c`
- Channel: tooba-main, WorkerId: tooba-worker-01, AgentType: cursor
- Parent-Task: TB-TMAR-NOTIFICATION-AMSC-001-W3-R2
- Historical lineage to preserve: W0 `5777ff4a` → W1 `c660b933` → W2 `e2f23975` → W3 `763dab13` → W3-R1 `7b8ab79e` → W3-R2 `028ef769`

## Architect verdict driving this task

W3-R2 Structure repair is ACCEPTED. Current tree: ZERO child directories under the four Customer/Seller request axes, all ten requests + six validators directly colocated, structureState = READY_FOR_CERTIFY, production behavior unchanged. The previous W3 certification remains historical evidence but is NOT sufficient for current HEAD (its Structure handoff was invalidated by the over-foldering defect). Run a fresh independent Certify pass → PASS (`COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`) or NOT_CERTIFIED with exact blockers.

## Required outcome (PASS fields)

Additive SoT `notificationModuleAmsc001W3R3` (state = NOTIFICATION_AMSC_001_RECERTIFIED, verdict = COMPLETE_REFERENCE_PATTERN, structureCertified = true, structureState = CERTIFIED, structureGateSource = TB-TMAR-NOTIFICATION-AMSC-001-W3-R2, folderGranularityState = PROFESSIONAL_SHALLOW, singleFileRequestLeafState = ZERO, perUseCaseRequestLeafState = ZERO, pathNamespaceState = EXACT, physicalCopyState = CLEAN, rootAllowlistState = ENFORCED, httpApplicability = HTTP_OWNING, endpointOwnershipState = MODULE_OWNED, routeCount = 10, endpointReachableRequests = 10, validatorRequiredCount = 6, noValidatorRequiredCount = 4, validatorCoverageState = EXHAUSTIVE, crossModuleBoundaryState = CONTRACTS_ONLY, foreignAppInfraDomainCoupling = ZERO, crossModuleJoinState = ZERO, crossModulePersistenceState = ZERO, schemaMigrationState = UNCHANGED, microserviceExtractable = true, blockingResidualDebt = ZERO, productionCodeChangedThisWave = false, guardsWeakened = NONE, baselinesWidened = NONE, recoveryFollowupRequired = RECORD_R1_7B8AB79E_AND_R3_FINAL_SHA_AFTER_THIS_COMMIT, workflowStop = USER_REVIEW_NOTIFICATION_AMSC_001_W3_R3, automaticNextImplementationTask = NONE). Master Recovery module-local recertification checkpoint. Manifest honest reconciliation for the current flat structure. Durable R3 guard strengthening (R2 drift cannot reappear). Evidence set (certification.md, validation.md, current-structure.md, validator-matrix.md, boundary-audit.md, recovery-handoff.md). One commit, push, `HEAD == origin/main`, Result to Bridge, STOP — no automatic Recovery wave, no next module.
