# Design

## Context

See `proposal.md` and `specs/story-slide-product-picker/spec.md`. The current shared editor is already rendered by `StoryManagementScreen` for admin and VendorShell seller routes. Product lookup, selection state, dirty detection, durable intent and the owning `update_item` command already exist in that file. This change is presentation and state emphasis, not a new data flow.

## Goals / Non-Goals

**Goals**
- Make the current product unmistakable in the actual slide dialog.
- Keep the existing selection stable through lookup/search/paging/error.
- Make options and controls readable and usable at 360px and with keyboard.

**Non-Goals**
- No new product search API, backend command, permission, schema or storefront change.
- No new notification pattern, modal framework or copy registry.
- No change to media replacement, upload recovery or story lifecycle.

## Decisions

1. Put the selected-product card above search, not below the results.
   Rationale: the operator's first question is "what is selected now?" A bottom caption forces scanning the whole list. The card uses the existing surface/border/primary tokens and a check icon.

2. Represent selected options with three signals: `aria-pressed`, a check icon and a visible "selected" text badge, plus restrained background/border.
   Rationale: colour alone is not accessible and can be missed on a phone. The text badge remains readable in both locales.

3. Keep selection in the existing `slideCtaTarget` and `selectedProductLabel` state; never derive it from the current page.
   Rationale: search and paging already replace `productOptions`; selection must be independent. A selected card can state when the product is outside the current results.

4. Use wrapping, not truncation, for product names; keep row hit targets at least 44px and add focus-visible rings.
   Rationale: Persian multi-word names carry identity at the end; truncating them makes different products look alike.

5. Keep loading, empty and error as separate branches with distinct test ids; retry remains inline and does not mutate selection.
   Rationale: a generic empty list hides a failed lookup and invites a wrong product choice.

## Risks / Trade-offs

- [Selected card adds vertical height on 360px] → Keep it compact, use wrapping and let the dialog body scroll naturally; do not pin it over the form.
- [Selected product outside the current page can look stale] → State "current selection" explicitly and add a short outside-results note only when the loaded page omits it.
- [Existing copy is shared with other Story surfaces] → Add keys to both fa/en catalogs and reuse them only in this picker.
- [The screen is already oversized] → Do not extract a new abstraction; keep the change local and test the existing selectors.

## Migration Plan

Frontend-only. No data or schema migration. Rollback is reverting the frontend commit. Existing pending intents remain compatible because the update-item envelope and occurrence path do not change.

## Open Questions

None.
