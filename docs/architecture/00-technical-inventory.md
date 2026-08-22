# Tooba — P00 Technical Inventory

Status:

```text
P00 discovery input — not locked architecture
```

Task:

```text
TB-P00-T001
```

This document separates (A) facts observed in the canonical repository from (B) Architect-verified facts about `shopeiva.zip`. Template routes and components are not Tooba product requirements.

## A. Canonical Repository — Observed Facts

Inspection after TB-P00-T000 commit `012e1a7dc9eb9a80944e61716da25a53dbd6d34c`.

```text
Repo-Root: D:/Users/User/source/repos/SarvNewVer
Canonical: https://github.com/mrnikiemami-code/Tooba
Branch: main
HEAD == origin/main: YES
```

### Top-level (tracked)

```text
AGENTS.md
README.md
SETUP.md
docs/
```

No application `src/`, `app/`, `package.json`, `*.csproj`, `go.mod`, `Cargo.toml`, `Dockerfile`, `.github/`, `docker-compose*`, or database/migration directories are present.

### Application source

```text
ABSENT
```

No frontend or backend product code.

### Documentation / pipeline structure (present)

```text
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/ai/pipeline-runtime-policy.json
docs/ai/tasks/TB-P00-T000.task.md
docs/pipeline/TASK-TEMPLATE.md
docs/pipeline/GATE-TEMPLATE.md
docs/pipeline/inbox/.gitkeep
docs/pipeline/results/.gitkeep
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/prompts/TOOBA-ARCHITECT-NEW-CHAT.md
docs/prompts/TOOBA-CURSOR-PIPELINE-START.md
```

Templates under `docs/pipeline/` are not executable envelopes.

### Build / tooling / runtime

```text
ABSENT (no package manager lockfiles, no language toolchain config)
```

### CI/CD

```text
ABSENT
```

### Docker / containers

```text
ABSENT
```

### Database / migrations

```text
ABSENT
```

### Tests

```text
ABSENT
```

### Secrets / config risks

```text
No .env, credentials, or secret files observed in the tracked tree.
No .gitignore file is present at repository root.
```

`.gitignore` is **not adequate** for a later application tree (node_modules, build outputs, env files would not be ignored). For the current docs-only bootstrap, there is no application artifact to ignore yet. This task does not add a `.gitignore`.

### Shopeiva / template code in repository

```text
NOT_PRESENT
```

No `shopeiva.zip`, extracted template, `public/images` storefront assets, or `src/app` template code.

## B. Purchased Template — Architect-Verified Facts

Source: Architect direct inspection of `shopeiva.zip`. Archive is **not in the canonical repository**. Cursor did not copy, unzip, or vendor it.

### Framework / package baseline

```text
Next.js 16.2.6
React 19.2.4
React DOM 19.2.4
Tailwind CSS 4
ESLint 9
eslint-config-next 16.2.6
```

### Relevant dependencies present (in the archive)

```text
axios
chart.js
framer-motion
fuse.js
lucide-react
next-themes
persian-date
persian-datepicker
react-chartjs-2
react-hook-form
react-loading-skeleton
react-otp-input
react-paginate
react-toastify
swiper
zod
zustand
```

### App Router

```text
src/app/
route groups and dynamic routes
approximately 73 page/layout/route entry files at Architect inspection time
```

### Storefront concepts present as template routes/pages

home, alternate home variants, product detail, categories, category/subcategory, brands, brand detail, search, cart, payment, offers/sale, best seller, most viewed, new products, trending, compare, gift card, coupons, premium, referral, seller list/profile, login, register, forgot password, static informational pages, blogs/article.

### Customer area concepts

dashboard, profile, addresses, orders, wishlist, wallet, gift cards, notifications, tickets, settings.

### Vendor area concepts

vendor registration, vendor dashboard, analytics, products, orders, customers, reviews, coupons, gift cards, wallet, tickets, settings.

### Data / assets in archive

```text
public/images/
public/fonts/
public/jsons/
```

Persian-oriented assets and date/font dependencies. Local demo JSON datasets.

### Interpretation (authorized)

The template is UI/UX implementation input + potential reusable frontend code/assets + technical migration/adaptation input.

It is **not** domain, backend, SEO, security, tenant, or authorization architecture truth.

### Template features that are not Tooba requirements

The following remain `TEMPLATE_PRESENT / PRODUCT_DECISION_PENDING` unless independently confirmed by USER:

```text
premium
referral
wallet
gift card
site survey
vendor capabilities
```

Presence of a template route does not create a Tooba requirement.

## C. Potentially Reusable Template Areas

Classifications are preliminary. No code-level integration analysis was performed in-repo (archive not present). Do not overstate reuse.

| Area | Classification | Note |
| --- | --- | --- |
| Component/layout structure | REUSE_CANDIDATE | Next.js App Router storefront layout likely useful as visual/structure reference |
| Responsive patterns | REUSE_CANDIDATE | Commercial template expected to include mobile layouts; not verified in-repo |
| Design tokens / theme (`next-themes`) | ADAPT_HEAVILY | Must later support theme-per-store; template theming is not tenant architecture |
| Navigation / header / footer | REUSE_CANDIDATE | Likely visual reuse; routing/SEO contracts must be Tooba-owned |
| Product cards / grids | REUSE_CANDIDATE | UI only; not Catalog Product vs Offer model |
| Product page UI | REUSE_CANDIDATE | UI only |
| PLP / search UI | ADAPT_HEAVILY | fuse.js in template is not PostgreSQL FTS / OpenSearch architecture |
| Customer dashboard UI | REUSE_CANDIDATE | Screens are not identity/authorization design |
| Vendor UI | REFERENCE_ONLY | Marketplace vendor area may inform UX; not authorization or offer model |
| Forms (`react-hook-form`, zod) | REUSE_CANDIDATE | Library choices may be reusable; not identity/OTP/MFA design |
| Charts | REFERENCE_ONLY | Dashboard chrome; analytics architecture is separate |
| State stores (zustand) | ADAPT_HEAVILY | Client state is not domain ownership |
| Validation (zod) | REUSE_CANDIDATE | Input validation only |
| Local JSON / demo data | REJECT_FOR_PRODUCTION | Demo datasets must not become catalog truth |
| Static SEO behavior | REJECT_FOR_PRODUCTION / ADAPT_HEAVILY | Template SEO is not Tooba SEO architecture |
| Routing conventions | ADAPT_HEAVILY | `src/app` conventions may inform later frontend; locale/tenant routing unresolved |
| Persian-only assumptions | ADAPT_HEAVILY | Product is multilingual; Locale != Market != Currency |
| Accessibility | UNKNOWN_REQUIRES_LATER_REVIEW | Not inspectable without the archive in-repo |
| Performance / CWV | UNKNOWN_REQUIRES_LATER_REVIEW | Not measured |
| Security | REJECT_FOR_PRODUCTION | Template auth/payment UI is not security architecture |
| Data fetching (axios) | ADAPT_HEAVILY | Must later go through Tooba contracts, not ad-hoc HTTP |
| Server/client component balance | UNKNOWN_REQUIRES_LATER_REVIEW | Archive not in repo |

## D. Risks / Gaps Against Tooba Requirements

Status values: `PRESENT` | `PARTIAL` | `ABSENT` | `UNKNOWN` | `NOT_APPLICABLE_AT_TEMPLATE_LAYER`

Observed against the **canonical repository** unless noted as template-archive only.

| Risk area | Status | Note |
| --- | --- | --- |
| multilingual / locale routing | ABSENT | No app source; template is Persian-oriented per Architect |
| RTL + LTR | ABSENT | Repo has no UI; template likely RTL-first, LTR unknown |
| SEO architecture | ABSENT | Confirmed requirement; not designed or implemented |
| metadata/canonical/hreflang | ABSENT | |
| structured data | ABSENT | |
| faceted navigation/indexation | ABSENT | Template search UI is not indexation policy |
| multi-market | ABSENT | |
| multi-currency | ABSENT | |
| marketplace Product vs Offer | ABSENT | Template seller pages are not this separation |
| single-store tenant/domain routing | ABSENT | |
| theme-per-store | ABSENT | `next-themes` in template is not tenant theming |
| SpiceDB authorization | ABSENT | |
| dynamic identifiers / OTP / MFA | ABSENT | Template has OTP input dependency; not Tooba identity |
| Keycloak readiness | ABSENT | |
| Content service | ABSENT | Template blogs/article is not Content module |
| composable landing pages | ABSENT | Template home variants are not a composition system |
| PostgreSQL search -> Elasticsearch/OpenSearch | ABSENT | fuse.js in template is client search demo |
| cache abstraction -> Redis | ABSENT | |
| OpenTelemetry | ABSENT | |
| advanced logging/audit | ABSENT | |
| first-party analytics | ABSENT | Template vendor analytics UI is not first-party tracking |
| media transformation pipeline | ABSENT | Static `public/images` is not a media pipeline |
| AI/RAG | ABSENT | |
| B2B foundations | ABSENT | |
| microservice-ready module boundaries | ABSENT | No modules exist |
| accessibility | UNKNOWN | Cannot audit archive from this repo |
| mobile quality | UNKNOWN | Cannot audit archive from this repo |
| Core Web Vitals/performance | UNKNOWN | Cannot measure; no app in repo |

## E. Template Adoption Principle (preliminary, non-ADR)

```text
Preserve valuable visual/front-end work where compatible,
but adapt through Tooba architecture rather than bending
Tooba architecture around the template.
```

This is a discovery conclusion, not permission to integrate Shopeiva.
