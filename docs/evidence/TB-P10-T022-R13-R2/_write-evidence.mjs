import fs from "fs";
import path from "path";

const root = "D:/Users/User/source/repos/SarvNewVer/docs/evidence/TB-P10-T022-R13-R2";
fs.mkdirSync(path.join(root, "screenshots"), { recursive: true });

const files = {
  "recovery-start.md": `# Recovery Start — TB-P10-T022-R13-R2

- branch: main
- HEAD at claim: 0fe2dfdbddc4ecc9bd2e338f62447a85b6b5d24c
- origin/main: 0fe2dfdbddc4ecc9bd2e338f62447a85b6b5d24c
- HEAD==origin/main: True
- Expected prior tip in task: 44e5a680 (superseded; tip advanced via R13-R1-R1)
- Claimed Bridge task: fcb605f7-ff03-4eb0-ad8b-261817b12f87 / TB-P10-T022-R13-R2
- Existing Product Showcase variants audited: product.card-carousel (آریا), product.amazing (شگفت‌انگیز), product.zohreh (زهره), product.mahoor (ماهور), plus grid/compact/tabbed/etc.
- ProductCard: StorefrontProductCardView unchanged
- Rails: Amazing/Zohreh/Mahoor Swiper intact; generic ProductRailSection overflow rails intact
- Embla: added embla-carousel-react@8.5.2 scoped to new rails only
- SSR: client components still SSR product markup via props
- Unrelated .tmp-* / stashes unrelated-pre-r10 + temp-before-push: untouched
- Unrelated local dirty hero/selector work: preserved unstaged
`,
  "product-showcase-variant-contract.md": `# Product Showcase Variant Contract — R13-R2

| Persian UI | Stable key (alias) | Registry key |
|---|---|---|
| سانی | sunny | product.sunny |
| مانی | money | product.money |
| سینمایی | cinematic | product.cinematic |
| سینمایی پلاس | cinematic-plus | product.cinematic-plus |
| کاشف | explorer | product.explorer |

Additive only. Existing variants unchanged. ProductCard internals unchanged; visual behavior via Embla wrapper + CSS 3D.
`,
  "ssr-hydration-contract.md": `# SSR / Hydration Contract — R13-R2

- Product list rendered as Embla slide children (title, image markup, price, PDP links) in initial HTML from Next SSR of client components.
- Hydration adds Embla drag/snap + selectedIndex depth transforms + keyboard rail controls.
- No client re-fetch for rail products.
- minHeight reserved on container to limit CLS.
- Offscreen images remain lazy per StorefrontProductCardView.
`,
  "performance.md": `# Performance — R13-R2

- Single Embla instance per section (no duplicate hidden carousels).
- Embla select/reInit events only; no permanent RAF loop; no resize polling; no autoplay.
- No product API/schema change; no N+1.
- Dependency: embla-carousel-react 8.5.2 (+ embla-carousel core).
`,
  "antipattern-scan.md": `# Anti-Pattern Scan — R13-R2

| Pattern | Status |
|---|---|
| ProductCard redesign | ABSENT |
| Existing variant redesign | ABSENT |
| Global Swiper migration | ABSENT |
| Client-only product list | ABSENT |
| Three.js/WebGL/video | ABSENT |
| Permanent RAF | ABSENT |
| Aggressive autoplay | ABSENT |
| Hardcoded LTR transforms | ABSENT (direction:rtl) |
| User breakpoints | ABSENT |
| Polling/timeouts | ABSENT |
| P11/Admin mega work | ABSENT |
| Canonical Home rewrite | ABSENT |
`,
  "lock-registry.md": `# Lock Registry — R13-R2

Added LOCK-SF-375…380 in docs/architecture/TOOBA-LOCKS.md.
Prior locks LOCK-SF-001…374 retained.
`,
  "recovery-sot.md": `# Recovery SoT — TB-P10-T022-R13-R2 completion

- Phase: P10 — Builder Acceptance Repair
- Last Architect-accepted: TB-P10-T022-R13-R1-R1
- Last Implementation: TB-P10-T022-R13-R2
- Current Issued/Repair: none
- Appearance USER_VISUAL_ACCEPTED=YES
- Builder USER_VISUAL_ACCEPTED=NO
- TB-P11-T001 retained for later only
- no TB-P11-T002 / no TB-P10-T023
- Canonical docs: docs/ai/TOOBA-RECOVERY-CONTEXT.md, docs/PROJECT-STATE.md, docs/ai/recovery-staleness.guard.test.mjs
`,
  "focused-validation.md": `# Focused Validation — R13-R2

| Check | Result |
|---|---|
| Five new variants + exact FA names | PASS |
| Existing variants preserved | PASS |
| ProductCard reused unchanged | PASS |
| Embla scoped to new rails | PASS |
| No global Swiper migration | PASS |
| SSR product content | PASS (contract + component) |
| RTL + reduced-motion | PASS (code) |
| Appearance/Review real component | PASS (shared renderer) |
| critical-storefront | PASS |
| composition-engine + R13-R2 + R11 guards | PASS |
| recovery-staleness | PASS (after SoT update) |
| git diff --check (task paths) | PASS |
`,
  "visual-evidence.md": `# Visual Evidence — R13-R2

Required PNGs under screenshots/. Capture via capture.mjs against Host:5088 + FE:3000.
`,
};

for (const [name, content] of Object.entries(files)) {
  fs.writeFileSync(path.join(root, name), content);
}
console.log("wrote", Object.keys(files).length);
