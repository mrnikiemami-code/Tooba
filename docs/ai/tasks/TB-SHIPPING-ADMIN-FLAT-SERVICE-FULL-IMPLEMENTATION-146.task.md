PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146
Parent-Task: TB-SHIPPING-INDEPENDENT-GROUP-SCHEDULE-145
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ADMIN-FLAT-SERVICE-CLOSURE
Mode: BOUNDED_REAL_ADMIN_IMPLEMENTATION_NOT_WIREFRAME
Status-Target: PASS_OR_PARTIAL_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Expected-Base-SHA: ad3ceb7a09929407673890111cb4f5e2c3277705
Timebox: 45 MINUTES HARD; at 35m reserve time for verification, report, push; stop at 45m and mark PARTIAL if incomplete. Do not chain new tasks.

READ THESE LOCKED AUTHORITIES FIRST

docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md
docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md
docs/planning/MUST-FIX-BEFORE-RELEASE.md
existing /admin/shipping-services page and current actual service/option, rates/coverage, schedule, packaging/freight-payment UI/API implementations.

USER DECISION / GOAL
NO MORE WIREFRAME OR DESIGN-ONLY TASK. Deliver the actual usable Shipping Admin page in the existing running app, for user visual and functional testing. The user explicitly wants the WHOLE page ready, not a mockup, another audit or a hollow shell. Preserve the current Admin design system, RTL and responsiveness; no global CSS redesign.

CANONICAL ADMIN MODEL
ONE CUSTOMER-SELECTABLE SHIPPING METHOD = ONE INDEPENDENT ADMIN-MANAGED SERVICE ROW, e.g. post:express, post:standard, tipax:express, tipax:standard, store_courier, in_person where these exist in real persisted configuration. Carrier/provider identity (Post/Tipax) is metadata, not a parent edit page with 'service types' children. Never merge separate service options' rates, availability, schedule, packaging or freight semantics. The UI identity must retain exact canonical serviceCode+optionCode mapping, preserve historical references, and use actual server capabilities. Do not fabricate new methods or data or silently turn shared parent policy into independent saved policies.

REQUIRED USER-VISIBLE OUTPUT (ALL REQUIRED FOR PASS)

/admin/shipping-services renders a flat, readable table with ONE ROW per real selectable service/method, localized service name, canonical code, status, provider label if useful, and only three per-row controls: View / Edit / Delete. Keep Add Service control; remove the clutter of separate rate, coverage, schedule and packaging row action icons and useless spacing/overflow. Preserve permissions and confirmation semantics, and accessibility labels.
View/Edit opens ONE selected method's own detail UI with FOUR NON-NESTED TABS: General; Rates & Coverage; Transport Schedule; Packaging & Shipping Fee. NO service-types tab and NO global tabs across the whole list. Do not use popup-on-popup nesting or forms that close unexpectedly. Dialog or routed detail acceptable if stable, scroll-safe, keyboard and RTL correct.
Fully functioning forms: existing create/edit/view/delete functionality, service identity/status, rates and coverage rules, transport schedule, packaging and freight payer settings are all reachable WITHIN their method-specific tabs using the real existing validation, save/delete/cancel, error and persistence APIs. Existing records must prefill correctly; save/reopen must show persisted data. Form availability must be honest: where backend supports only provider-wide rate/schedule/packaging but not per-method isolation, DO NOT falsely display shared data as per-method or write sibling method settings. Implement safe exact method filter/scoping using existing option keys where supported; otherwise report a concrete BLOCKED acceptance item with source proof and do not mark PASS.
Existing provider/option read/write contracts, code snapshots, Checkout / Order / Fulfillment / Settlement shipping identities MUST NOT regress. NO destructive migration or ad-hoc provider-to-method data duplication. Preserve existing policies and historical orders.
Demonstrate real browser UI (not an invented wireframe): screenshot main table, one selected Post Express detail with all four tab contents, saved/reopened change where safe, and browser console/network check. If shared localhost processes/DB are already running, USE them read-only unless explicitly authorized; never restart or mutate shared tooba_alpha, :3000/:5088 or unrelated processes. If unable to test safe write on a private isolated disposable environment, disclose it and mark corresponding verification incomplete.

EXECUTION STRATEGY / PRIORITY IN ONE TASK

First 10 minutes map concrete Admin screens, reusable forms, action handlers, and actual API method-vs-provider scoping. Do not produce a separate research-wave task; start implementation within current task.
Implement flat rows and stable method detail tabs through existing components, then relocate/reuse the functioning forms (not copy/paste business logic). Keep per-method scope in state and server calls; permission/error logic unchanged.
Run only focused frontend checks and actual safe browser acceptance. No full rebuild/restart/test suites without need.
If actual persisted backend scoping cannot meet the contractual independence in 45m, finish the working safe UI portion and report PARTIAL with exactly which tabs/controls cannot safely persist and why; NEVER misrepresent scaffold or screenshots as functional completion. Do not branch into N+1, scheduling task 145 implementation, commercial settlement allocations or new services.

PROTECT LOCAL WORK

Existing dirty src/frontend/app/admin/admin-fulfillment-work-queue-screen.tsx and admin-order-detail-screen.tsx, two evidence logs, and all pre-existing stashes are unrelated. Do not overwrite, reset, stash, autostash, rebase or force-push them. If main advanced, use a separate clean worktree to implement and integrate safely. Preserve concurrent code. Check branch and origin SHA before writes; report actual Base SHA.
Respect Host composition, Contracts-only module boundaries, strict service/method IDs, no client authority over delivery price or scheduling. No real customer data edits.

ACCEPTANCE REPORT (USER TO TEST)
Report exact page URL, what is done vs blocked, actions and tabs successfully tested, actual screenshots and paths, targeted test results, all changes/commit SHA, proof origin/main==HEAD if pushed, and specific 3-minute manual steps the user can execute. Must STOP_FOR_ARCHITECT. PASS iff all five user-visible requirements are objectively met and no sibling-method policy leakage occurs. Otherwise PARTIAL/BLOCKED and wait. NO AUTO FOLLOW-UP.
END_TOOBA_TASK