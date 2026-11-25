# RESULT — TB-296-R9-FINAL-OFFER-WORKSPACE-CLOSURE

TASK: TB-296-R9-FINAL-OFFER-WORKSPACE-CLOSURE
STATUS: PASS_WITH_LIMITATIONS (worker; owner UAT remains external)
BASE: 6426349f7c4d0a6b4423e64a370156a067a88a28
BRANCH: opencode/iso-agents-002
IMPLEMENTATION_SHA: bd05747712f2ebd022bd0bd8f89559f9eac58d6a
START: 2026-11-25 17:18:53 +03:30 (session user message)
END: 2026-11-25 18:52:46 +03:30 (implementation commit timestamp)
DURATION: 01:33:52

## Delivered

- R9-01: R8 approved seven-tab/4-route/2-AppDataGrid layout preserved; runtime lists at 360/768/1440 have zero overflow; no redesign or shared-grid replacement.
- R9-02: shared `react-toastify` ToastContainer (`autoClose=3000`) for transient success; no persistent success banner; shared design-system `MessageBox` for duplicate/link, uncertain/reload, dirty-exit and partial-create recovery; inline field validation retained. Permanent rule added to `.cursor/skills/tooba-frontend-feature/SKILL.md` and `docs/architecture/NEXTJS-DESIGN-FIDELITY.md`.
- R9-03: blank inventory reason rejected inline, valid correction saved 199→50, obsolete error cleared, human warehouse name `انبار نمایشی تهران` shown, fixture restored to 199 and independently reread.
- R9-04: no hardcoded channel select; create/duplicate key audited (seller+variant+channel); system channel shown as read-only policy with pending-policy note. No allowed-channel owner contract exists, documented instead of invented.
- R9-05: pricing/conditions and admin shipping unlocked after valid identity before persistence; one explicit save created exactly one Draft and applied price/conditions through owning clients; seller panel same for price; partial create keeps the created Draft link. Inventory/history remain gated because no eligible-location provisioning/history contract exists; no fake position.
- R9-06: real Catalog unit code/name, decimal places, step and rounding read into offer detail/picker; conditions no longer shows «از محصول»; client step/precision validation added; Offer create/update validates quantity limits against Catalog.
- R9-07: actual DB registry via `/v1/admin/languages` and additive read-only `/v1/languages`; actual `?locale=ru-RU` offer route with `dir=ltr`, `lang=ru-RU`, Russian tabs, Russian list title, registry formatting; live registry not mutated. Global shell routing remains project fa/en; shared grid chrome stays on the existing shared fa/en grid locale API.
- R9-08: recovery key now actor/tenant/owner scoped; shipping survives language/reload remount; dirty cancel MessageBox + discard; stale `baseLastUpdatedAt` recovery rejected; successful save/create clears draft.
- R9-09: all changed domains reread; partial inventory preserves remainder; network unknown blocks retry and offers reload; saving flags clear in finally; stale pre-read blocks overwrite. No RowVersion/ConcurrencyToken exists in Offer/Pricing/Inventory; documented, not claimed atomic.
- R9-10: real two-position offer used; second write aborted; first=10 saved, second draft=20 and reason preserved; retry sent only remainder; final [10,20]; restored [0,0] and reread.
- R9-11: explicit isolated IDs/baselines; try/finally cleanup; only `TB296-R9-` offers archived; temporary seller-B role/assignment/ceiling removed; cleanup JSON ok true.
- R9-12: build/typecheck/lint; 26 mount; 16 API; 15 model/i18n; 97 Offer; 11 Catalog/Localization/Stage2 guards; orders/products/campaigns regression; runtime 32/0; cross-owner 404/404/403; OpenSpec strict; Impeccable audit.

## Verification

- FE build `QtUkz110oTvxQPkjamMqI`; Host/FE on 5088/3000; `/health/ready` ready (SingleStore, inmemory authorization, messaging Healthy).
- Runtime JSON: `evidence/r9-runtime/verify-r9.json` SHA-256 `F1B78DFA9C1A921C521616DA5A13185984962FD578AC9E2EA63FECE83E550F12` (32 checks, 0 errors).
- Cleanup JSON: `evidence/r9-runtime/cleanup.json` SHA-256 `932561400F096C616955AE5309A1D9ECA1D46476DDAF06A1E8360FFD108A138D` (ok true; R9 offers 0; roles/assignments empty; ceiling 0; baselines restored).
- Result matrix: `ACCEPTANCE-MATRIX-R9-RESULT.json` SHA-256 `A02706E77E89F7B46E76EBAC043340187B25E98C90AF218D2167FAC860ECD2AD`.
- Impeccable: source `[]`; URL 12 findings, 1 workspace nested-cards warning, rest admin shell/sidebar.
- No screenshots in chat; screenshots only in evidence.

## Limitations / external decisions

- Channel authority by edition/seller/permission/policy is not defined; no channel choice/SingleStore behavior was invented.
- Pre-create inventory provisioning and offer history contracts are absent; those tabs remain honestly gated.
- Atomic optimistic concurrency is absent in Offer/Pricing/Inventory; pre-read is a safety check, not compare-and-swap.
- Global shell and shared grid chrome remain fa/en by existing project routing; offer-owned surfaces use registry override.
- Owner UAT/visual acceptance remains external. TB-297+ was not started.

STOP_FOR_ARCHITECT
