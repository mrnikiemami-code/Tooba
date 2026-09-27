PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-HOST-CACHING-ARCHITECT-DIRECT-001
Parent-Task: TB-TMAR-HOST-DEVELOPMENT-AMC-001
Parent-Commit: 0139fb89fcf7dfe48e957bb4aad62a575a41413f
Channel: tooba-main
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER
Execution: ARCHITECT_DIRECT
Target-Host-Folder: src/backend/Host/Tooba.Host/Caching
Title: Direct Analyze → Migrate → Certify Host/Caching

Scope:
- exact Host/Caching folder only;
- direct Program/BuildingBlocks/test/evidence/SoT references only;
- no whole-Host scan;
- no next Host folder.

Decision:
- generic platform cache infrastructure may remain Host-owned;
- do not create a Cache/Caching business module;
- retain only module-agnostic platform configuration/telemetry/composition/provider code;
- repair bounded composition defects found during analysis.

Verified defect:
- Program registered CacheHostOptions and CacheOptionsValidator but did not call AddToobaCache().

Required result:
- retain four legal Host/Caching platform files;
- add AddToobaCache() once to Program;
- add durable exact-folder/cache-boundary guard;
- preserve cache behavior, tenant/edition isolation, telemetry, provider policy;
- no schema/migration/frontend change;
- persist evidence/SoT;
- do not claim runtime test PASS unless actually executed.

STOP:
Do not inspect/start next Host folder.
END_TOOBA_TASK
