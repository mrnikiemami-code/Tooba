# Frontend recovery roadmap — TB-TMAR-FE-BASELINE

## Phase FE-F1 — guards + low-risk shared fixes
- Candidates: host/csrf helper centralization; stop new admin-api growth; remove dead reverse imports when safe.
- Risk: low. Tests: architecture guards + existing unit. Rollback: git revert commit.
- Benefit: freeze debt before moves.

## Phase FE-F2 — admin feature extraction
- Candidates: category-admin, product-workspace, content-article, landing composer, split admin-api.
- Risk: medium-high. Requires characterization (see characterization-plan). 
- Benefit: end flat accumulation.

## Phase FE-F3 — storefront feature ownership
- Move `app/storefront` implementation toward `features/*` while keeping thin routes.
- Risk: medium (SEO/visual). Tests: critical-storefront.

## Phase FE-F4 — giant decomposition
- Split CRITICAL files with characterization first (ARCH-REFACTOR-001 / FE).
- Risk: high. Benefit: maintainability.

## Phase FE-F5 — server/client SEO optimization
- Reduce contagion on Home/PLP/PDP/article.
- Risk: medium. Benefit: SEO/perf.

## Phase FE-F6 — dependency consolidation
- Swiper/Embla; CKEditor/TipTap decisions; antd containment.
- Risk: medium visual. Benefit: bundle clarity.

Physical folder moves only after ownership + import boundaries understood.
