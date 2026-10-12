PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-FASHION-PREVIEW-BRAND-LOGO-NULL-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE
Repo: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: dc3e551407876becb3e9ca5333d4ff8b87fcb042

GOAL
Remove exactly the TS2322 diagnostic at src/frontend/lib/storefront-composition/fashion-demo-preview.ts:216 arising because the mapped brand's logoMediaAssetId is string | undefined, while StorefrontBrandItem declares logoMediaAssetId: string | null. Preserve the established absence semantics for fashion/sample preview and store preview. Expected diagnostics: 15 -> 14, with no new diagnostics. Compiler green is NOT claimed.

MANDATORY PREFLIGHT
Verify git HEAD == origin/main == Expected-Starting-HEAD; tracked working tree clean. If diverged, STOP and report. Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md. Read the actual brand producer/mapping code around lines 125-155, 175-225, 500-545 of fashion-demo-preview.ts, the StorefrontBrandItem contract in app/storefront/storefront-model.ts, relevant consumers/tests, and docs/architecture/TOOBA-LOCKS.md applicable preview locks. Do NOT read or edit deferred ARCHITECTURE-FINDINGS-REGISTER.md.

ARCHITECT OBSERVATION TO VERIFY, NOT ASSUME
The consumer-owned StorefrontBrandItem currently declares logoMediaAssetId: string | null. The fashion mapper at line ~221 currently emits b.logoMediaAssetId ?? undefined, apparently contradicting that contract. Investigate whether absence legitimately maps to null, and whether replacing only the undefined fallback with null is truthful and keeps the media URL / preview-fake / Store-vs-Sample semantics intact. Trace consumers and confirm no required distinction between absent and null is lost. Avoid changes to general brand contract.

STOP GATE
If undefined is a materially distinct required state for consumers or a locked preview invariant, STOP with BLOCKED_ARCHITECT_DECISION and evidence; do not widen StorefrontBrandItem, cast, or invent fallback asset IDs. Otherwise implement the minimum correct mapping inside fashion-demo-preview.ts only. Be transparent about any intentional runtime difference (missing property value undefined -> null) and demonstrate it cannot alter existing locked behavior, or STOP.

ALLOWED EDITS
Prefer the single production file src/frontend/lib/storefront-composition/fashion-demo-preview.ts, at the brands projection. A tiny focused existing test amendment is allowed only for meaningful domain behavior, not ceremonial string matching. Add exact task receipt to docs/ai/tasks/TB-FE-FASHION-PREVIEW-BRAND-LOGO-NULL-001.task.md and bounded evidence under docs/architecture/evidence/TB-FE-FASHION-PREVIEW-BRAND-LOGO-NULL-001/.

FORBIDDEN
Do not edit StorefrontBrandItem or shared schema/API/Host/backend. No storefront Home/PDP/Shopeiva visual, composition renderer, brand carousel layout, template catalog provenance, sample/store isolation or fake-marked contract changes. No fake logo IDs, no any/casts/non-null assertions/ts-ignore/suppressions, no tsconfig/CI/test baseline/guard weakening, no broad refactor, no unrelated TS fixes, no AMSC, no broad test suite, no retry cascade, no automatic follow-up task.

VALIDATION
Run frontend node_modules/.bin/tsc --noEmit exactly once after final edits. Compare to baseline docs/architecture/evidence/TB-FE-FASHION-PREVIEW-ORIGIN-TYPE-001/typecheck-after-tsc.txt using normalized file+line+error-code keys; target TS2322 absent and zero new diagnostic keys expected, 15 -> 14. Run only the nearest directly relevant pre-existing focused fashion-preview/brand projection tests or guards, selecting a small bounded set. Record failing pre-existing guards honestly and DO NOT chase unrelated stale paths. If scope changes are runtime-visible, add one genuinely behavioral test if a suitable existing seam exists; no ceremonial tests.

EVIDENCE AND HANDOFF
Commit task receipt and minimal summary.md, typecheck-before.txt, typecheck-after-tsc.txt, focused-tests.txt (or accurate alternative names); include proof of consumer/absence behavior, exact diff, remaining count, test totals and limitations, rollback criteria, and changed files. Commit and push to origin/main with no force/reset/stash. Confirm HEAD == origin/main. Return one complete BRIDGE-WAKE-V1 Worker result with Commit SHA, results, and STOP. Do not initiate next task.
STOP
END_TOOBA_TASK
