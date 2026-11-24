# Architect agent routing — owner decision 2026-11-24

This is an architect-facing recovery rule, not an always-on worker prompt or a new product architecture. Before issuing or reviewing any task, identify the actual recipient and verify its remote branch, HEAD, current task and last evidence. Never assume main is the OpenCode worktree.

## OpenCode (light isolated agent)
- Branch: opencode/iso-agents-002; Windows worktree: D:\Users\User\source\repos\SarvNewVer-opencode-iso-002. Verify actual branch and remote tip before task issuance. Do not switch to main or merge main wholesale.
- Read the short branch AGENTS.md, docs/opencode/ARCHITECT-DELIVERY-METHOD.md (currently TOOBA-OPENCODE-DELIVERY-V1@1.3.0), relevant iso-rules and latest task artifacts under docs/opencode/tasks/. Use configured metisdeepseek/deepseek-v4-flash unless owner explicitly changes it.
- Keep context targeted; only load applicable task-type rule packs and skills. No historical dumps, repeated logs, screenshots, speculative refactors, or redundant tests. Use .opencode/instructions/concise-chat-output.md; final handoff exactly eight lines. Evidence stays in repo files.
- Architect prepares immutable TASK.md, matching REQUIREMENTS.json and ACCEPTANCE-MATRIX.json before dispatch for new implementation tasks, with stable IDs, exact base, source-grounded scope, atomic positive/negative/boundary cases and independent oracles. Validate coverage and hashes; use OpenCode-specific verifier and mutation controls, never Cursor check-readiness as OpenCode acceptance.
- OpenSpec strict for applicable behavior; Impeccable critique/design/post-check for actual UI changes; Playwright connected proof for persistence UI. Evidence-only work uses existing specs and smallest sufficient proof.
- For the current Offer Stage-2 closure, owner requires existing Host 5088, Next 3000 and tooba_alpha only; no new Host/DB/Testcontainers. Verify safe fixture isolation, existing backup, authorized write/restore scope and code/build identity before writes. Do not interpret this task-specific restriction as permission to mutate shared data or as a blanket future policy. Preserve historical evidence.
- One connected matrix, at most one consolidated correction and one final rerun; no automatic next task. Stage 3 requires explicit architect acceptance and issuance.

## Cursor (main)
- Branch: main; verify real checkout and remote parity. Read full main AGENTS.md, docs/ai/TOOBA-RECOVERY-CONTEXT.md, pipeline documents and docs/architecture/TOOBA-DELIVERY-ASSURANCE.md. Use applicable repository .cursor/skills and existing architecture locks.
- Use existing BRIDGE-WAKE-V1, channel tooba-main, downloadable exact Task-ID.task.md, Cursor task/evidence directories under docs/ai and docs/architecture/evidence as prescribed, and existing plan/review check-readiness gate. Do not replace Cursor governance with OpenCode manifest/verifier or eight-line output requirements.
- Keep one active task, bounded execution and independent architect review. Do not import the isolated OpenCode AGENTS.md into main.

## Shared invariants
One product architecture; domain ownership/Contracts, CQRS, security, DB-backed locale authority, RTL/LTR, and design system remain mandatory. Real owning write plus independent reread for persistence; Worker PASS is not Architect ACCEPT or owner acceptance. No automatic AMSC, queue, task progression, cross-branch overwrite or unauthorized data changes. Resolve conflicting historical pointers from actual branch tip and evidence, not chat memory. Record any blocker honestly and STOP_FOR_ARCHITECT. This routing rule does not accept any outstanding task or authorize Stage 3.
