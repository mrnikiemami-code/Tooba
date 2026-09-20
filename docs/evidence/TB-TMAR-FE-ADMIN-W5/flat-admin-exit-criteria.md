# Flat-admin exit criteria — TB-TMAR-FE-ADMIN-W5

## Dimensions

A. Folder structure: FE-FOLDER freezes green; migrated features in `features/`; residual flat files still include oversized aggregators.

B. admin-api: still holds orders/dashboard capability debt + SHARED formatters (LOC 1022).

C. source size: admin-screens 894 and admin-api 1022 remain >800; require more migration or godfile characterization.

D. boundaries: public boundaries + deep-import guard cover 6 migrated features.

E. tests: discovery active; characterization for migrated slices present; orders not characterized for extraction.

## Verdict

Flat-Admin-Exit-State: NOT_READY

Stop ADMIN-Wx only when admin-api residual is SHARED_TECHNICAL-only (or explicitly deferred orders package) AND major flat aggregators are <800 OR explicitly queued as godfile tasks.
