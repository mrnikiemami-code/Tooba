PIPELINE-PROTOCOL: ARCHITECT-DIRECT-AMC
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-IDENTITY-AMC-001
Parent-Task: NONE
Channel: tooba-main
AgentType: cursor
Program: TMAR — Identity AMSC
Mode: ANALYZE_MIGRATE_STRUCTURE_CERTIFY
Track: IDENTITY_COMPLETE_REFERENCE
Title: AMSC Tooba.Identity.* to COMPLETE_REFERENCE_PATTERN / microservice-extractable
Backend-Only: YES
Skills: tooba-architecture-analyze, tooba-architecture-migrate, tooba-architecture-structure, tooba-architecture-certify
Waves: W0-Analyze, W1-Slnx, W2-LayerStructure, W3-EndpointsEvacuate, W4-CqrsResult, W5-Structure, W6-Certify
Host-Final-Closure: PRESERVE
END_TOOBA_TASK
