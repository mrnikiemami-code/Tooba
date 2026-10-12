PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-FE-LANDING-RESOURCE-DATE-LOCALE-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FRONTEND-TYPECHECK-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE
Repo: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: 6f8f6ff4aae1df4be14d7c25a268e5502584c550

OBJECTIVE
Repair exactly TS2554 at src/frontend/app/admin/landing-pages/admin-resource-selector.tsx:741: formatJalaliDate(row.publishDate) supplies one argument while the canonical formatJalaliDate(iso, locale: "fa" | "en") in src/frontend/design-system/app-data-grid/jalali.ts requires two. Current typecheck baseline is 20 errors. Target 19 with no new diagnostics.

MANDATORY PREFLIGHT
Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md, and the exact diagnostic. Verify clean tracked worktree and HEAD == origin/main == Expected-Starting-HEAD; STOP on conflict. Do not read the deferred findings register. Inspect the full relevant portion of admin-resource-selector.tsx, its display locale sources, canonical formatter implementation, and closest existing date-formatting callers. Determine the correct locale for THIS UI through evidence, not guesswork. Do not change the formatter's global signature/default.

ALLOWED PRODUCTION SCOPE
Only src/frontend/app/admin/landing-pages/admin-resource-selector.tsx. Make the smallest sound locale-argument repair consistent with this selector's existing language policy. Preserve publishDate null fallback, date data, search/filter/sort, grid columns, modal/resource selection, markup and Shopeiva/Home/PDP contract. If the correct locale cannot be established without a product or architecture decision, STOP with BLOCKED_ARCHITECT_DECISION; do not invent a locale or silently change behavior. Do not change any other production file.

FORBIDDEN
No broad i18n refactor; no formatter implementation/API change; no any, unsafe casts, ts-ignore or suppression; no tsconfig/CI/baseline/guard change; no backend/schema; no storefront composition/Home/PDP edits; no AMSC, retries, broad test suites, speculative related repairs, or automatic next task.

VERIFY AND EVIDENCE
Run frontend tsc --noEmit exactly once after editing; compare diagnostics to the previous evidence docs/architecture/evidence/TB-FE-RESERVATION-POLICY-NUMERIC-FIELD-KEY-001/typecheck-after-tsc.txt, normalized by file/line/code to avoid union order noise. Run one nearest relevant pre-existing focused suite if reasonably available; never claim unrun tests pass. If no focused suite, explain and use compiler + source-contract verification. Preserve pre-existing unrelated failures and report them; no cascade.

COMMIT AND STOP
Add exact task receipt at docs/ai/tasks/TB-FE-LANDING-RESOURCE-DATE-LOCALE-001.task.md, plus brief summary, before/after tsc and focused test output under docs/architecture/evidence/TB-FE-LANDING-RESOURCE-DATE-LOCALE-001/. Commit and push to main without force; confirm HEAD == origin/main and give exact SHA, changed files, chosen locale with justification, compiler before/after, tests and limitations. STOP for independent Architect ACCEPT. Do not implement another task.
END_TOOBA_TASK
