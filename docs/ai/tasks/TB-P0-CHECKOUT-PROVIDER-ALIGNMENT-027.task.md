TB-P0-CHECKOUT-PROVIDER-ALIGNMENT-027

Task-ID: TB-P0-CHECKOUT-PROVIDER-ALIGNMENT-027
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-FINAL-CLICK-API-026
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: d6dde48974f5b4c315aa868179c33d7df7be9da7
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 20 minutes HARD
automaticNextImplementationTask: NONE

Mandatory preflight
Verify HEAD == origin/main == Expected-Base-SHA; if drift, STOP with evidence, do not auto-rebase. Read AGENTS.md, BRIDGE-WAKE-V1, task 026, and 018/019/025 sources. Preserve all unrelated dirty/untracked changes. No Host or Host.Tests edit/build/test/stop/restart/deploy, no live DB writes, no PSP/wallet transaction. Host at :5088 must remain untouched.

Mission — solve blocker #2 of 026 first, in production code, not just docs
Make 019 StartPayment Composer resolve the selected gateway alias to the exact canonical provider chosen by Payment BEFORE 018 hold preparation. The effective hold policy used by 018 MUST match PaymentUnpaidTimeoutAssigner's concrete ProviderCode policy (including per-provider override). Do not hardcode default provider, vendor names, durations, fallback hours or new TTL. Preserve wallet/manual semantics, identity, actor/guest handling, 021 idempotency semantics, and all existing storefront provider-offering rules. Prefer an existing Payment.Contracts port/catalog abstraction. If a minimal additive Payment.Contracts resolver is needed, own implementation/DI in Payment module; Order only references Contracts. Fail closed on unavailable/disallowed selector: no checkout commit, no hold extension, no money. Do not silently pick another provider. Ensure provider eligibility is ultimately enforced by existing Payment authority.

Crucial ordering: provider resolution/eligibility MUST happen before canonical commit when it can fail. Then pass the same canonical code into Order 018 preparation and Payment 012R initiation. For gateway selector, do not pass bare 'gateway' to preparation. If preserving selector at Payment initiation is required for authoritative offering checks, design a typed resolution carrying canonical provider + trusted original selector, and prove both resolve to the same provider without duplicating policy; otherwise STOP with pinpoint blocker, no insecure workaround. Ensure idempotent replay with a changed chosen method still gets 021 conflict rather than a wrongly replayed payment.

Verification
Use only focused Payment and Order module tests. Prove provider-specific override is applied to hold preparation when selecting 'gateway'; method choice aligns to actual Payment provider, wallet/manual unchanged, invalid method has ZERO commit/PSP calls, same-key replay preserved, no new duration literals. Do not claim Host runtime proof from fakes. Keep task <=20 min; implement safe bounded slice only. If correct implementation cannot fit or requires Host edits, STOP BLOCKED with precise minimal follow-up. No endpoint or frontend publication in this task.

Governance for blocker #1 of 026
The accepted 019 test No_module_or_host_route_publishes_start_payment is a TEMPORARY inertness-phase guard. Record in evidence that it must be transitioned, in the future API publication task, to assert exactly ONE approved Order storefront final-click route and zero Host routes. Do NOT weaken or edit it in this task because no route is published. The ISender-only storefront route guard also requires a targeted contract transition at that future step, not blanket deletion.

Result contract
Return BEGIN_TOOBA_WORKER_RESULT / END_TOOBA_WORKER_RESULT with exact SHA(s), changed paths, actual test counts, selector-vs-concrete proof, any remaining blockers, zero Host changes, STOP_FOR_ARCHITECT. No automatic next task.
