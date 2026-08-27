PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_WORKER_RESULT

Task-ID:
TB-P06-T026-R1

Channel:
tooba-main

Status:
PASS

Summary:
Final proof Repair for Wallet/Gift Card: full backend/FE validation (0 warnings/errors/failed/skipped), seed idempotency, browser captures for Customer Wallet/Gift/Admin + Shopeiva comparison routes, concrete USER-PREVIEW URLs, runtimes kept alive. Minor Dev seed fix adds `TOOBA-DEMO-GIFT-R1` when prior demo codes are consumed. Wallet projects added to Tooba.slnx. No functional redesign of ledger/UI.

Recovery:
predecessor: 9ec56d15069cf080940829d93cbf01beef7d8091
HEAD: (filled after push)
origin/main: (filled after push)
tree: clean tracked after commit

Backend-Validation:
restore: PASS
build: 0 warnings / 0 errors
tests: Passed 290 / Failed 0 / Skipped 0 (Host.Tests 286 + MigrationRunner.Tests 4)

Frontend-Validation:
typecheck: PASS
lint: PASS (0 warnings/errors)
customer/wallet/admin suites: PASS (npm test + test:wallet)
production build: PASS

Seed:
Development only: yes
idempotent: yes (restart did not duplicate credits)
balance: 1100000 after spare redeem
ledger: 4 entries
unused/spare Gift Card: TOOBA-DEMO-GIFT-R1 (01900000-0000-7000-9000-000000000026)
expired: rejected 400
duplicate redemption: idempotent

Browser-Customer:
Wallet: captures/05-customer-wallet.png + 05b mobile
balance: 1,100,000
history: yes
mobile: yes
fake deposit: absent
fake cards: absent

Browser-GiftCard:
valid redeem: proved (API + UI form)
balance after: 1100000
duplicate: safe
invalid/expired: safe reject
feedback: UI success/history

Browser-Admin:
Gift Cards: captures/07
detail: 07b partial seeded card
redemption history: yes
revoke: control present
Wallet inspect: 07c ledger-derived + audited adjust

Shopeiva:
runtime: :3001 alive
exact comparison URLs: /user-panel/wallet , /user-panel/gift-cards
captures: 08 / 08b (shell; login required for full panel content)
foreign UI: none
unsupported fake actions: not ported on Tooba

Visual-Fidelity:
CSS/JS/animation/transition/hover: preserved Shopeiva-locked wallet/gift geometry on Tooba
responsive: mobile capture
unauthorized deviation: none intentional

Runtime:
Backend: http://127.0.0.1:5088
Frontend: http://127.0.0.1:3000
Shopeiva: http://127.0.0.1:3001
health/live: 200
health/ready: 200
kept alive after Result: yes

USER-PREVIEW:
Customer Wallet: http://localhost:3000/customer-panel/wallet
Customer Gift Card: http://localhost:3000/customer-panel/gift-cards
Admin Gift Cards: http://localhost:3000/admin/gift-cards
Admin Gift Card: http://localhost:3000/admin/gift-cards/01900000-0000-7000-9000-000000000022
Admin Wallets: http://localhost:3000/admin/wallets (actor aaaaaaaa-aaaa-4aaa-8aaa-000000000009)
Original Shopeiva: http://127.0.0.1:3001/user-panel/wallet and http://127.0.0.1:3001/user-panel/gift-cards
dev preview identity/context: Host Development; customer actor aaaaaaaa-aaaa-4aaa-8aaa-000000000009; unused demo code from GET /v1/admin/wallet/demo-preview → TOOBA-DEMO-GIFT-R1
preview steps: open Customer Wallet → confirm balance/history → Gift Cards redeem with demo-preview code → Admin gift list/detail → Admin wallets load demo actor

Source-of-Truth:
P06: IN_PROGRESS
T026: REPAIRED_BY_TB-P06-T026-R1
T026-R1: AWAITING_ARCHITECT_ACCEPT
Wallet: LIVE
Gift Card: LIVE
checkout: DEFERRED
refund: DEFERRED
seed: LIVE_DEV_ONLY
browser proof: LIVE
user preview: READY
visual contract: SHOPEIVA_LOCKED

Git:
commit: test prove wallet and gift card preview [TB-P06-T026-R1]
push: origin main
final HEAD: (after push)
origin/main: synchronized
tracked tree: clean

Architectural-Concerns:
Shopeiva authenticated user-panel content requires login; comparison used live routes + source lock. Checkout wallet spend and refund-to-wallet remain deferred.

Visual-Concerns:
None beyond Shopeiva login-gated panel body (shell captured).

Blockers:
NONE

Claims:
WALLET_GIFT_CARD_FINAL_PROOF_PASS
WALLET_USER_PREVIEW_READY
NOT USER_VISUAL_ACCEPTED / PRODUCT_FULLY_READY / PRODUCTION_GO_LIVE_READY / WALLET_CHECKOUT_LIVE / REFUND_TO_WALLET_LIVE

END_TOOBA_WORKER_RESULT
