PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-096
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-095
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_LEGACY_ACTIVATION_AUTHORITY_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: c5a72f59b05b5a7c985c3737b5c5f890f9b43496
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
095 BLOCKED_ACTIVATION_AUTHORITY is accepted. The operative activation authority remains Fulfillment legacy shipping_services.is_active and shipping_service_options.is_active, because Order storefront reads them. The one-level shipping_methods.is_active is an admin/catalog projection and MUST NOT become an independently competing activation authority. This task addresses writer consistency BEFORE exposing any toggle. Do not activate post:express or snapp_courier in real Development. For option-backed methods, a narrow identity-preserving update to an existing legacy ShippingServiceOption is acceptable only through one Fulfillment-owned transactional authority that also aligns the mapped one-level method row. For optionless services, activation means service-level state and must NOT be guessed or implemented in this task. Preserve storefront eligibility and existing order identities. No Checkout price cutover.

PREFLIGHT / CLAIM
Claim exactly one pending task via BRIDGE-WAKE-V1, preserve received docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-096.task.md as UNSTAGED receipt. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, 093–095 decisions/evidence and latest master recovery. Fetch origin/main; expected SHA must be ancestor, with any drift proved to be strictly nonoverlapping result bookkeeping. If overlapping src drift, STOP RECOVERY_CONFLICT. Preserve all pre-existing dirty/untracked paths; never reset/stash/rebase/force-push. Identify current Dev provenance before any read; all task DB activity must be isolated or read-only on Development.

MISSION A — CLOSE THE DUAL-WRITER HAZARD (PRIORITY)

Inspect ShippingServiceDirectory.UpdateAsync and all actual callers/admin endpoints; establish how its wholesale remove/reinsert of options can undo activation and change option UUIDs. Pick the smallest compatible repair ensuring existing option IDs and activation cannot be accidentally reset when unrelated legacy service details are edited. Do not invent a second independent writer or silently discard legitimate explicit option edits. If maintaining backward compatibility cannot be proved within scope, STOP with explicit PARTIAL/BLOCKED and a precise next safe step.
Implement the narrowest Fulfillment-owned option activation domain/application/infrastructure operation, if and only if the legacy writer conflict is resolved first. Input: immutable one-level methodCode and requested IsActive only; resolve its immutable legacy binding server-side. Require existing active parent, option-backed method, and existing option row; refuse unknown, inactive parent, optionless, mismatched binding, missing table, invalid code or gate OFF. No client-provided serviceCode, optionCode or option UUID. Update ONLY the specific existing legacy option activation plus associated one-level method activation in ONE transaction, preserving sibling options and UUIDs. Existing legacy ShippingServiceOption.SetActive is the domain setter. No migrations or permissions bypass.
If enough time remains, expose ONE module-owned authorized CQRS/ISender + Result/ApiResponseFactory endpoint for the narrow operation; otherwise stop after source guard/reconciliation and report exactly what is not complete. No frontend toggle in 096. A meaningful operation must have actual storefront eligibility parity; updating only shipping_methods is forbidden.

MISSION B — TARGETED PROOF
Run focused Fulfillment tests for no sibling mutation, preserved option UUID and names, explicit legacy writer update consistency, atomic success/rollback (isolated DB if needed), gate-OFF zero DB calls, inactive parent refusal, optionless refusal, absent/stale binding, unchanged historic method codes, and idempotent no-op. Avoid Host output DLL locks and broad test suites. Inspect relevant architecture guards if route count/validator count changes, update narrowly and run only if safe. No live Development activation DML, no Host/Next restart, no changing gates, no seed, no production/staging contact.

ALLOWED PATHS
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain//ShippingService.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/*
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/**
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs (only if needed)
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs (only DI)
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/** (focused)
src/backend/Host/Tooba.Host.Tests/Architecture/FulfillmentModuleAmsc001W3CertGuardTests.cs (narrow guard update only)
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-096.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-096/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-096.task.md (UNSTAGED RECEIPT ONLY)
Any necessary path outside these must be reported for Architect approval, not silently added.

FORBIDDEN
No frontend edits, service-level optionless activation, automatic activation of inactive services, full-replace option rewrite, independent one-level toggle detached from legacy, Order/Checkout/Payment/Settlement writes or pricing change, snapshot changes, new schema, live Development DML, unauthorized restarts, broad suite loops, force push/reset/stash/rebase. No credentials in evidence.

ACCEPTANCE / STOP
PASS only if the dual-writer risk is demonstrably contained and a narrow transactionally consistent option-backed activation operation is proven by focused tests. PARTIAL if only one safe portion is completed. BLOCKED if a safe single authority cannot be established. Report actual elapsed; HARD STOP at 30 minutes, no extension. Do not issue 097 automatically. STOP_FOR_ARCHITECT.

RESULT CONTRACT
Return exactly one canonical BRIDGE-WAKE-V1 worker result with Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row, Status, Stop-State, Expected-Base-SHA, Base-SHA, Code-SHA, Evidence-SHA, automaticNextImplementationTask: NONE, exact changed paths, tests actually run, safety/gate state, authority proof, remaining issues and elapsed time.
END_TOOBA_TASK
