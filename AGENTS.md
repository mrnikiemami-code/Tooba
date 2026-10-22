# Tooba — Agent Rules

## Active Shipping recovery — 2026-10-22

Chat-loss checkpoint: the latest section at the top of the recovery document
records accepted work, completed R8, unresolved gates and the exact continuation.
R8 is completed and Architect-accepted for metadata only; do not replay it.

For the current Shipping Admin work, start at
`docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md`.
TB-190 including R1/R2 is accepted for the bounded owner/region draft foundation.
TB-191 manual rate draft persistence is accepted through TB-191-R1.
TB-192 owner-local multilingual draft metadata is accepted through TB-192-R1.
TB-193 weekly schedule draft persistence is accepted through TB-193-R1.
TB-194 independent freight/calculation draft settings are accepted (authoring only).
TB-195 type ownership analysis is accepted with Architect corrections (rules section 26).
TB-196 Fulfillment type catalog/API/Contracts/schema/storage is ACCEPTED through R13-R1.
This is bounded catalog acceptance, not live Shipping integration, UI, release or AMSC recertification.
TB-196-R1 remains PARTIAL; its bounded Domain regressions are repaired and accepted through R2.
TB-196-R3 API evidence is accepted through R4; registry/tracing/real-DI/relational gates remain open.
TB-196-R5 DI/registry/listener evidence and its single-active correction through R6 are accepted.
TB-196-R7 naming/isolated UpDown repair is accepted; legacy/snapshot and full relational gates remain blocked.
TB-196-R8 canonical snapshot/latest TargetModel metadata alignment is accepted.
TB-196-R9 historical analysis is accepted with Architect corrections (model manifest191, not216).
TB-196-R10 pending-outbox-index migration and bounded isolated proof are accepted.
TB-196-R11 existing CHECK/name model alignment is accepted; five defaults remain.
TB-196-R12 five-default model alignment and strict schema parity are accepted with recorded deviations.
TB-196-R13-R1 repaired winner/full-tree evidence is accepted; catalog proof stage closed.
Only next issued task: TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197,
Shipping-owned assignment via existing Fulfillment.Contracts, server-authorized owner, shared Revision.
TB-197 was executed on 2026-10-22 and its result is posted to Bridge; it is NOT Architect-accepted yet.
TB-197-R1 (post-commit lookup repair + focused same-context/provider-failure/DI+parent-trace proof) was
executed on 2026-10-22 from `cc24c9c1` and posted to Bridge; NOT Architect-accepted. Await review only.
No shared deployment or quote activation. Follow rules section27; do not replay the catalog tasks.
Do not replay historical tasks or resume TB-185 performance. Fetch current HEAD
and verify actual task state;
this is a recovery pointer, not a new implementation authorization.

## Current accepted checkpoint — 2026-10-23

TB196 bounded catalog and TB197 owner-local type-assignment backend are Architect-accepted through197-R1.
Read latest recovery/reviews for exact evidence/process limits; no merchant UI/quote/release acceptance.
TB198 source analysis accepted with corrections; read its ARCHITECT-REVIEW.md before implementation.
TB199 implemented at be639a473700d771cbf7b3f90f011814cb82bfeb; proof completion required, not closed.
Only next issued task: TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199-R1 (parent199), tests/docs only.
Read TB199 ARCHITECT-REVIEW.md: new-route auth/catalog sentinel, complete storage snapshots/token/noop time and exact transaction caller/shape proof.
No production/frontend/schema/quote/activation change; no task200 while199-R1 unfinished.
Latest recovery is authoritative; do not replay accepted196/197/198.
Do not replay old active-task paragraphs as authority; this block and latest recovery supersede them.

## Core rules

1. One active Task only.
2. Execute only the Task actually received from Bridge on the configured channel
   after `BRIDGE-WAKE`.
3. `Worker PASS != Architect ACCEPT`.
4. Bridge is the transport/orchestration boundary; the repository is durable
   technical Source of Truth.
5. The Coding Agent is normally **IDLE / OFFLINE** between Tasks.
6. Do **not** poll Bridge continuously while idle.
7. Do **not** require permanent online Worker presence or idle heartbeat.
8. `BRIDGE-WAKE` is infrastructure control traffic, not a Task or Result.
9. `SYSTEM-BRIDGE-ALERT` is not a Result and must not be treated as one.
10. Do not emit or interpret an alert merely because the Worker is offline
    between Tasks.
11. Do not invent requirements, broaden scope, or redesign locked architecture.
12. Do not self-authorize the next Task.
13. Do not force-push, rewrite history, silently stash, or destroy unrelated
    work.
14. Do not modify unrelated files.
15. Do not mark Architect ACCEPT unless the Task explicitly authorizes it.
16. Do not replay historical task files from `docs/ai/tasks/` as current work.
17. Home and PDP are critical Shopeiva-locked surfaces. Any Task touching shared
    storefront components must run `npm run test:critical-storefront` and follow
    `docs/visual-baselines/CRITICAL-STOREFRONT-VISUAL-REVIEW.md`. Functional PASS
    does not imply Visual ACCEPT.
18. **Never POST test, probe, or placeholder payloads to Bridge `/api/results`.**
    Only the complete Task Result contract (`BEGIN_TOOBA_WORKER_RESULT` …
    `END_TOOBA_WORKER_RESULT`) after validation, commit, and push. Do not probe
    the Results API with dummy `content`.

## Current pipeline

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
CHANNEL: tooba-main
```

```text
ARCHITECT
→ downloadable .task.md
→ Tampermonkey
→ Bridge Task = Pending
→ External Watchdog
→ BRIDGE-WAKE
→ Coding Agent wakes
→ claim exactly one Task
→ implement
→ Result
→ Bridge
→ Tampermonkey
→ ARCHITECT
→ ACCEPT / REPAIR / BLOCK
→ next .task.md
```

## Worker lifecycle on BRIDGE-WAKE

1. recover `main` and verify `HEAD == origin/main` on a known/safe tree;
2. read governance and recovery docs;
3. check Bridge health;
4. claim exactly one Pending Task on the configured channel;
5. persist the received downloadable `.task.md` artifact;
6. execute only that Task;
7. validate, commit, push, and post the complete Result through Bridge;
8. complete the Bridge task lifecycle;
9. return to **IDLE** and stop.

## Retired transport

The following are historical only:

- manual Architect envelopes pasted into a Coding Agent;
- same-session or same-chat continuation;
- Cursor browser/composer/send-button transport;
- requiring one specific agent product;
- **BRIDGE-V2 continuous polling while idle**;
- **permanent idle heartbeat as a normal prerequisite**;
- **Worker waiting loop between Tasks**.

Historical task/result artifacts may retain legacy syntax as evidence only.

## Source of Truth

Before normal execution:

```text
branch = main
HEAD == origin/main
known/safe working tree
```

After successful execution:

```text
local commit
push origin main
git fetch origin
HEAD == origin/main
clean working tree
```

Unsafe divergence is `RECOVERY_CONFLICT`.

## Frontend feature architecture — mandatory

For EVERY feature addition or change under `src/frontend/**`, BEFORE coding read
`.cursor/skills/tooba-frontend-feature/SKILL.md` and
`docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md` and follow its ownership,
dependency-direction, API/state/UI, type-safety, focused validation, and stop rules.
This is required even when a task only says "fix" or "add a feature"; merely asking
the agent to act as a senior architect does not replace the repository contract.
Preserve `ARCH-FE-FREEZE-001`, existing visual/API/behavior locks and Task scope.
A conflict or out-of-scope refactor requires STOP and Architect approval.
Never claim a page is certified just because a feature builds.

## Backend feature architecture — mandatory

For EVERY authorized feature addition or bug fix under `src/backend/**`, BEFORE
coding read `.cursor/skills/tooba-backend-feature/SKILL.md` and follow its
module ownership, CQRS, Result/error/localization, validation, Contracts-only,
DI, security, and focused-test rules. The feature skill prevents new debt; it
does not replace the architecture skills or authorize AMSC recertification.
Preserve existing contracts and locked state; conflicts require STOP and
Architect approval. Never silently broaden scope or weaken architectural guards.

## Deferred audit findings — opt-in only

Do NOT read or scan `docs/architecture/ARCHITECTURE-FINDINGS-REGISTER.md`
during ordinary tasks, feature development, bug fixes, or screen audits.
Do NOT automatically add audit findings to it. The register is consulted
ONLY when the Architect/user explicitly requests adding, updating, reviewing,
or resolving entries in that register. Before adding an entry, search existing
entries for duplicates and update the existing ID rather than creating another.
Keep access targeted (search relevant IDs/terms before reading large sections)
to minimize context usage. The register never authorizes tasks, AMSC waves,
baseline resets, or scope expansion.

## Agent skills

Repository-owned skills live under `.cursor/skills/<name>/SKILL.md` and are
versioned in git, so they survive a Cursor reset, cache clear, or machine change.
After a reset, read the durable registry `docs/ai/AGENT-SKILLS.md`, then open the
matching `.cursor/skills/<name>/SKILL.md` and follow it exactly.

## Shipping Admin rules — mandatory reference

Before designing or changing shipping Admin/seller rules, read
`docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md` (`SHIPPING-ADMIN-RULES-001`).
Preserve one owner-scoped control panel, shipping type above method name, one
non-overlapping multi-region selection with multiple condition/price rows beneath
it, and reuse of that geography by packaging and scheduling. Payment timing and
rate calculation mode are independent. Do not reintroduce duplicated geography,
raw location IDs, cross-owner inheritance or seller-only customer grouping.
Multilingual fields, configured locales, shared non-translated rules, and RTL/LTR
are mandatory (decision sections 10–11). The TB-186-R2 visual preview baseline was accepted by the owner on 2026-10-22.
Decision sections 13–15 add authoritative single-store/marketplace capability
handling and database-backed configured languages. Backend delivery is authorized
only through the single active Bridge task and repository AMSC/feature skills;
visual acceptance is not production activation, integration acceptance or module
certification. Do not hardcode two locales, trust browser owner/mode, or create
parallel mode/language authorities.
This records approved requirements, not implemented capability or permission to
weaken guards; report migration/contract conflicts explicitly.

## Shipping customer experience — mandatory reference

For shipping-related Admin, seller or Storefront work affecting customer delivery
groups, costs or delivery timing, first read
`docs/architecture/shipping/SHIPPING-STOREFRONT-UX-REFERENCE.md` and inspect its
linked user-provided image. Preserve `SHIPPING-STOREFRONT-UX-001`: clear delivery
groups, per-group cost and independent delivery date/window choices backed by real
availability. Never substitute raw technical codes or seller-only grouping for
this approved experience. The reference is a target, not implementation evidence
or permission to bypass existing locks. Changes to its approved UX require an
explicit product-owner decision.

## References

- `docs/ai/AGENT-SKILLS.md`
- `docs/ai/TOOBA-PIPELINE-PROTOCOL.md`
- `docs/ai/TOOBA-PIPELINE-CONTROLLER.md`
- `docs/ai/TOOBA-RECOVERY-CONTEXT.md`
- `docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md`
- `docs/PROJECT-STATE.md`
- `docs/ROADMAP.md`
