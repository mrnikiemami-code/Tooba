# Tasks

## 1. Shared picker presentation

- [ ] 1.1 Add fa/en picker copy for selected title, selected badge, current-outside-results hint and localized load-more progress; verify TypeScript and catalog access.
- [ ] 1.2 Add the selected-product card above search with `aria-live`, a check icon, current name and a current-outside-results note; verify the existing picker mount test still opens the picker.
- [ ] 1.3 Mark selected options with `aria-pressed`, check icon and selected badge, wrap long names, and preserve selection through search/paging/error; verify the focused picker tests.

## 2. Mobile and keyboard quality

- [ ] 2.1 Make retry and load-more controls 44px, keep search/selected/list readable at 360px, and add focus-visible treatment; verify tsc and the targeted mount suite.

## 3. Connected browser acceptance

- [ ] 3.1 Add a task-scoped Playwright config and controlled-HTTP fixture for admin save/reload, cancel, 503 retry and mobile long-name geometry; verify tests run on real Chromium.
- [ ] 3.2 Add seller-role and fa/en 1440px smoke assertions against the same shared picker; verify the seller route uses only its fixed shop id.
- [ ] 3.3 Run the browser suite once, self-review, apply at most one correction round, then rerun affected assertions.

## 4. Evidence and delivery

- [ ] 4.1 Record before/after evidence, geometry/transport JSON, test commands and limitations; verify the review gate against actual files.
- [ ] 4.2 Run picker tests, tsc and production build; verify the served app still starts.
- [ ] 4.3 Commit only relevant changes, push, verify origin parity and report PASS/PARTIAL/BLOCKED honestly.
