BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version:
1

Task-ID:
TB-P05-T009-REPAIR-01

Parent-Task:
TB-P05-T009

Phase:
P05 — Operational Surface Integration

Status:
ISSUED_REPAIR

Title:
Repair Demo Catalog Depth, Brand Seed, and Mega Menu Evidence

Architect Review:
TB-P05-T009 functional/public-route work is directionally correct, but the RESULT is NOT ACCEPTED yet because the authorized addendum TB-P05-T009-ADDENDUM-01 was not demonstrated as completed.

The parent RESULT explicitly reports:

current seed has no brand rows

brand listing evidence is an honest empty state

no evidence was reported for the required 8 top-level category demo hierarchy

no evidence was reported for 3 child categories per top-level category

no product-count matrix for those categories was reported

Therefore:

TB-P05-T009 = REPAIR_REQUIRED

This repair is narrow.
Do NOT redesign.
Do NOT reopen already-passing public route architecture.

CRITICAL UI CONTRACT:

SHOPEIVA STRUCTURE = LOCKED UI CONTRACT

DO NOT INTERPRET THE TEMPLATE.
DO NOT REIMAGINE THE TEMPLATE.
DO NOT SIMPLIFY THE TEMPLATE.
DO NOT SUBSTITUTE COMPONENTS.

PORT / REUSE THE EXISTING STRUCTURE.
REPLACE DATA BINDINGS ONLY.

Repository Recovery:

Run first:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

4e3cba5975d3bd04abd6ede6b145830b664d5089

Require:

branch = main
HEAD == origin/main
safe/known working tree

No force push.
No destructive reset.
No silent stash.
No history rewrite.

Primary Repair Goal:

Provide sufficiently rich Development/demo data so the existing Shopeiva Mega Menu, category landings, brand pages, and product rails can be visually evaluated as a real sellable storefront.

This is DEMO/DEVELOPMENT SEED DATA.

Do not alter production bootstrap semantics.

MANDATORY CATEGORY MATRIX:

Create at least 8 distinct top-level categories.

Each top-level category must have at least 3 meaningful child categories.

Each child category must have at least 3 published demo products unless a hard architecture constraint prevents it.

Target:

8 top-level
× 3 child categories each
× 3 products each
= target 72 demo products

Required category families:

محصولات دیجیتال

گوشی موبایل

لپ‌تاپ

هدفون و صوتی

لوازم خانگی

نوشیدنی‌ساز

پخت‌وپز

نظافت

مد و پوشاک

پوشاک مردانه

پوشاک زنانه

کفش

زیبایی و سلامت

مراقبت پوست

آرایشی

بهداشت شخصی

خانه و آشپزخانه

ظروف پخت‌وپز

سرو و پذیرایی

دکوراسیون

خودرو و موتور

لوازم خودرو

قطعات مصرفی

لوازم موتورسیکلت

ورزش و سفر

ورزش خانگی

کمپینگ

کیف و چمدان

کتاب، هنر و سرگرمی

کتاب

لوازم تحریر

بازی و سرگرمی

PRODUCT SEED RULES:

Use realistic demo product names.

Examples are allowed, but exact naming is not mandatory.

For every demo product needed on Storefront product cards, seed the required commerce truth through owning modules/contracts:

Catalog Product
Offer
Pricing
Inventory

Preserve:

Product != Offer != Price != Inventory

Forbidden:

Product.Price
Product.Stock
cross-module SQL join
direct table writes that violate module ownership
frontend fake pricing
frontend fake inventory

Where a product should demonstrate marketplace behavior, seed seller/offer diversity through existing accepted contracts.

Do not invent a new Variant domain in this repair.

BRAND DATA — MANDATORY:

The previous RESULT says there are no brand rows.

That is not acceptable for visual verification of Shopeiva brand surfaces.

Seed a meaningful Development/demo brand set.

Minimum:

8 distinct brands

Prefer brands naturally aligned with the category/product matrix.

Examples:

Xiaomi
Samsung
Apple
Lenovo
ASUS
Bosch
Philips
JBL

Equivalent realistic alternatives are acceptable.

Requirements:

Brand listing must be visibly populated.

At least several demo products must be linked to brands.

Brand landing/detail must visibly show live associated products.

Do not invent production marketing claims.

Logo/media may use existing approved media seam or honest placeholder if asset infrastructure is not available; do not fake proprietary claims.

MEGA MENU:

The Mega Menu must remain navigation-only.

Required visual proof:

at least 8 top-level category choices visible/reachable
each selected top-level category demonstrates >= 3 real child categories

Forbidden:

product cards inside Mega Menu
prices inside Mega Menu
stock inside Mega Menu

Category links must lead to real live category listings.

PUBLIC ROUTE RECHECK:

After seeding, recheck:

/new-products
/brands
brand landing/detail
/sellers
seller profile
/offers
/sale

Best Seller / Trending / Most Viewed honesty rules remain unchanged.

Do NOT fabricate analytics signals just because seed data is richer.

EVIDENCE — REQUIRED:

Update/create:

docs/evidence/TB-P05-T009/

Required repair evidence:

11-mega-menu-8-top-level.png
12-mega-menu-child-depth.png
13-category-landing-populated.png
14-brands-populated.png
15-brand-landing-populated.png
16-seeded-product-rail.png
17-mobile-mega-menu-390x844.png
18-demo-seed-matrix.md

18-demo-seed-matrix.md must contain an explicit table with:

Top-level category
Child category
Published product count
Representative product names
Brand coverage
Offer count
Price present through Pricing module
Inventory present through Inventory module

Also record totals:

top-level category count
child category count
published demo product count
brand count
offer count

ACCEPTANCE THRESHOLDS:

Minimum:

top-level categories >= 8
child categories >= 24
published demo products >= 72
brands >= 8

If an exact >=72 product target cannot be met because of a REAL architectural constraint, stop and return BLOCKED with exact evidence.

Do not silently reduce the target.

SEED SAFETY:

Development/demo only.

Seed must be:

deterministic
idempotent or safely repeatable
environment-scoped
clearly separated from production defaults

Do not create uncontrolled duplicate data on every startup.

TESTS:

Add/verify focused tests for:

seed repeatability
8+ top-level categories
24+ child categories
72+ published products
8+ brands
brand-product association
category descendant listing
Mega Menu receives hierarchy, not product merchandising

Run full validation again.

Backend:

dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx

Require:

warnings = 0
errors = 0
failed = 0
skipped = 0

Frontend:

cd src/frontend
npm ci
npm run typecheck
npm run lint

Run all repository-supported suites, including:

storefront
public merchandising
customer
seller
admin
cart
checkout/order
payment
grid
workspace
product workspace

Then:

npm run build
git diff --check
git status --short --branch

SOURCE OF TRUTH:

Update:

docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md

Record:

TB-P05-T009 = AWAITING_ARCHITECT_ACCEPT
TB-P05-T009-REPAIR-01 = COMPLETED_BY_CURSOR
P05 = IN_PROGRESS

Do NOT mark Architect ACCEPT yourself.

PDP FOLLOW-UP LOCK:

Do not implement full PDP completeness in this repair.

But preserve this locked next-step requirement in recovery docs:

Backend capability must sit in the correct Shopeiva PDP section.

Required future PDP mapping includes:
- variants/options
- short/summary description
- full/detailed description
- media/gallery
- specifications/attributes
- seller/offers
- pricing
- availability
- reviews/ratings when capability exists
- related products

If an important real backend capability has no Shopeiva section,
add only the minimum Shopeiva-compatible section required.

This is an approved exception to strict no-structure-change, but only for missing important backend capability.

SAVE ENVELOPE VERBATIM:

docs/ai/tasks/TB-P05-T009-REPAIR-01.task.md

GIT:

git diff --check
git status --short --branch
git add ...
git commit -m "fix complete storefront demo catalog depth [TB-P05-T009-REPAIR-01]"
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Require:

HEAD == origin/main

RESULT CONTRACT:

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version:
1

Task-ID:
TB-P05-T009-REPAIR-01

Parent-Task:
TB-P05-T009

Status:
PASS | FAIL | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- predecessor:
- final HEAD:
- origin/main:
- synchronized:
- working tree:

Seed-Safety:
- environment scope:
- deterministic:
- repeatable/idempotent:
- production impact:

Seed-Counts:
- top-level categories:
- child categories:
- published products:
- brands:
- offers:

Mega-Menu:
- top-level count:
- child depth:
- navigation-only:
- category links:

Brands:
- brand count:
- populated listing:
- populated landing:
- product associations:

Commerce-Separation:
- Product.Price:
- Product.Stock:
- Product/Offer separation:
- Pricing authority:
- Inventory authority:
- cross-module SQL join:

Evidence:
- 11:
- 12:
- 13:
- 14:
- 15:
- 16:
- 17:
- 18:

Tests:
- seed repeatability:
- category count:
- child count:
- product count:
- brand count:
- brand associations:
- descendant listing:
- Mega Menu hierarchy:

Validation:
- backend restore:
- backend build:
- warnings:
- errors:
- backend passed:
- failed:
- skipped:
- npm ci:
- typecheck:
- lint:
- frontend suites:
- frontend build:
- git diff --check:

Source-of-Truth:
- parent status:
- repair status:
- P05:
- PDP follow-up preserved:

Git:
- commit:
- push:
- final HEAD:
- origin/main:
- synchronized:
- final status:

Architectural-Concerns:
- ...

Visual-Concerns:
- ...

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1

CRITICAL — RESULT DOES NOT END THIS AGENT SESSION

After RESULT:

PIPELINE_STATE: WAITING_FOR_ARCHITECT_IN_SAME_SESSION

DO NOT CLOSE OR LEAVE THIS SESSION.
WAIT HERE FOR THE NEXT AUTHORIZED ENVELOPE.

Do not invent TB-P05-T010.

Cursor PASS != Architect ACCEPT.
No Envelope = No Execution.
Repository = Source of Truth.

END_TOOBA_CURSOR_TASK_V1
