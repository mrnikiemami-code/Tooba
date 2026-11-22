# Impeccable critique — TB-285 Stage 2

Command: `impeccable detect` from the installed 4.10.0 lab.
Evidence: `IMPECCABLE-SOURCE.log`, `IMPECCABLE-AFTER.log`.

## Before (source and page observation)

- The seller create route exposed one `<select>` fed by the legacy `ListPublishedVariantsAsync` call, which returns variants of the newest 100 published products, so an older published item was unreachable.
- The seller list rendered one whole-array grid query in the browser.
- The admin shell had no Offer-create route.
- The seller form had no visible create-intent/resume state; a lost response left the operator without a truthful recovery path.
- The served admin page detection found low-contrast placeholder text for the new seller/product search fields and pre-existing 10px admin-nav microtext. The nav finding is in the shared AdminShell, not in the Stage-2 form.

## Design decisions

1. Keep AdminShell and VendorShell; add an Offers item inside the existing Products group and a seller route inside the existing VendorShell.
2. Replace the single select with one search field and a selectable result list: product title, Catalog code, `aria-pressed`, visible selected card, next page and total.
3. Put the seller identity above the product picker in admin; the seller is an explicit data choice, not an impersonated session.
4. Make Draft the first visible state. The form says activation is separate and does not imply checkout readiness.
5. Separate the primary action from Cancel; the primary is 44px, Cancel is the existing secondary control.
6. Use a durable create-intent token per shell+seller+variant. A partial step is a compact amber explanation with a link to the same Draft, not a generic success or a second create.
7. Use the existing red/blue product language and the existing table/grid patterns.
8. Add fa/en copy at page level; source registry remains the language authority. Do not introduce a new language engine.

## Post-check (final served build `UYWZYt9rgDXhubR29wG8o`)

- `impeccable detect` on the final admin and seller create pages: no placeholder-contrast finding from the new forms after the placeholder classes were added.
- Remaining 12 anti-patterns are the pre-existing shared-shell 10px admin-nav text and one pre-existing shell label; they are not introduced by the new forms and are reported, not silently relabelled.
- Advisory findings: selected admin row blue text 4.49:1 and a secondary label 4.4:1. No new form control is below the 44px primary target.
- Playwright final geometry: 1440/360 fa/en have `scrollWidth == innerWidth`, `dir` rtl/ltr correctly, and both primary create buttons are exactly 44px high. Details are in `connected-285.json` REQ07.

## Limits

- The shared AdminShell's 10px nav is a pre-existing visual debt outside this Stage-2 form.
- The product title text returned by the seed is Persian and was visually observed in the page; the console encoding in tool output does not preserve that glyph text, but the JSON/SQL identity does.
- No screenshot was taken; the evidence is DOM/geometry and tool output.
