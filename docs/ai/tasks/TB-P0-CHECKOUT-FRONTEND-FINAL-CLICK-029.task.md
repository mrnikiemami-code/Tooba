TB-P0-CHECKOUT-FRONTEND-FINAL-CLICK-029

Task-ID: TB-P0-CHECKOUT-FRONTEND-FINAL-CLICK-029
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-FINAL-CLICK-API-028
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 47f4d8f0ed86cf28efa67111f5e5e50b1ca7f83a
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 25 minutes HARD. automaticNextImplementationTask=NONE.

MANDATORY PREFLIGHT
Verify HEAD == origin/main == Expected-Base-SHA, else STOP and report without rebase/stash. Read AGENTS.md, BRIDGE-WAKE-V1, latest recovery and accepted 011/019/025/027/028 source and evidence. Preserve unrelated dirty files. Host CLOSED: NEVER edit, build, stop, restart, test or deploy src/backend/Host/** or running Development Host :5088. No live database mutation, migrations, PSP/wallet calls, real orders, backend deployment, or host compilation.

MISSION: USER-VISIBLE CHECKOUT FIX
On Shipping -> Payment navigation, persist shipping choice/draft only, then navigate WITHOUT invoking POST /v1/storefront/shipping/commit, converting cart, reserving stock, or creating a checkout. On Payment render a cart-addressed read-only draft using GET /v1/storefront/cart/{cartId:guid}/payment-draft (011) and preserve the selected shipping and method. Only explicit final user click on Pay invokes POST /v1/storefront/cart/{cartId:guid}/start-payment (028), passing expected cart version, stable click idempotency key, selected method/useWallet, coupon as appropriate, and canonical guest proof from existing headers/cookie. Never accept a money amount or checkout ID from browser; backend owns amount/stock/shipping.

BOUNDED STEPS

Trace actual Shipping continue handler, storefront payment page, API wrappers, and cart state/version storage. Identify all eager-commit callers. Make a concise source-grounded route map in evidence.
Change Shipping continue to save shipping draft, retaining Active cart; navigate to cart-addressed Payment URL/state rather than checkoutId-only payment. Refresh/direct navigation and guest/auth handling must work without local-only ephemeral state where possible.
Payment page: load the read-only payment draft by cart ID, display persisted shipping/amount and available tender selection via existing canonical Payment APIs (do not fabricate catalog/quote); preserve expected cart version. Explicit Pay click alone calls new start-payment route; disable duplicate-click UI and reuse the SAME stable idempotency key until the user intentionally starts a distinct attempt. Do not generate a fresh key per React re-render or retry of the same operation.
Handle outcomes: 200 online redirect; wallet/manual outcomes using existing supported payment UI flows; 4xx problem details with localized text; post-commit failures preserve checkoutId from problem.details and offer recovery on the SAME order. For expired/released hold, do NOT silently initiate payment; show actionable retry/reacquire or return to cart according to existing permitted backend flow. Do not invent renewal limits or hardcode three retries. Do not claim automatic renewal exists unless backed by API.
Keep current checkoutId-addressed Payment experience available ONLY where needed for existing persisted checkout recovery, never automatically re-commit shipping. Ensure old and new routes cannot both initiate a payment for one click. No unconditional service worker/timeout-driven side effects.

SAFETY GATES

Before Pay click: zero shipping/commit call, zero start-payment call, zero payment-initiation call, zero cart conversion.
If API contract cannot provide actual cart-addressed payment draft including method eligibility and usable shipping projection, STOP with a precise bounded blocker; do not replace real draft with fake checkout IDs or infer payment values.
If frontend change cannot be completed safely inside the timebox, finish only a coherent non-money UI slice with focused proof and report remaining wiring; do NOT publish an unsafe Pay button or pretend the user-visible defect is fixed.
Never change Host, backend Order/Payment/Inventory logic, schema, duration/expiry/retry settings, payment provider APIs, certified guards outside the touched frontend surface. No production deployment.

VERIFICATION
Only focused frontend tests/build typecheck relevant to changed slice; avoid broad suites, looped test reruns, and touching running Host files. Mandatory focused checks for Shipping Continue no commit, Payment draft load on cart URL, final click exactly one start-payment request with stable key, guest/auth identity protection, failure/recovery and online/manual/wallet routing as supported. If existing test harness cannot verify, report UNVERIFIED; do not claim runtime proof. Keep a short exact changed-files + test-results + blocker list.

WORKER RESULT
Return BEGIN_TOOBA_WORKER_RESULT / END_TOOBA_WORKER_RESULT with BASE and final code SHA, exact paths, precise Shipping -> Payment -> final-click behaviour, any remaining eager commit/money paths, tests and their real counts, Host CLOSED attestation, no deployment, STOP_FOR_ARCHITECT and automaticNextImplementationTask=NONE. Never auto-start another task.
