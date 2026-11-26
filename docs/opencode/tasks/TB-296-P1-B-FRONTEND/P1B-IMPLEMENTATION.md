# P1-B implementation checkpoint

Status: READY_FOR_INTEGRATION (internal worker handoff; not architect acceptance/owner UAT)
Source: 3aac14d1ab5bc31959b5a32853ebc5f0be43c5fe (rebased evidence over target; product entry db7bb1bd3)
Scope: frontend only. No backend, contract, matrix, guard or acceptance-runner edits.

## Implemented
- Central RegistryLocaleProvider from `/v1/languages`: one shared active/default resolution with full UrlPrefix/culture/direction/calendar, explicit fallback, unknown/disabled fallback, multi-character panel prefixes, shared Toast direction and document lang/dir. Existing public SEO locale routing preserved. No feature-local provider.
- Offer contract: real offer/price/offeredQuantity revisions, inventory location revision, nullable offered/allocation-held, capabilities; admin/seller mappings; price/stock saves use owner-specific revisions; offered-quantity transport (`/offered-quantity`) with operationId; fulfillment/commercial use offer revision.
- Platform seller UI: offered quantity editable; physical stock disabled/read-only; capability-gated warehouse/shipping controls. Seller fulfilled remains physical/location gated by server.
- Offered quantity local validation, exact reread, draft preservation and dirty integration; no fake stock IDs/revisions, no guessed currency fallback in affected detail/create reads. Existing AppDataGrid, Toast 3000ms, MessageBox, seven tabs and R8 layout preserved; no redesign.
- Localized `offer.fulfillment.change_protected` in fa/en/ru; existing history invariant keys preserved.
- Added focused tests: multi-character registry prefix, platform offered/physical lock; existing model/API/mount/i18n suites retained.

## Verification
- Frontend typecheck PASS.
- Next build PASS.
- Node focused 31+ PASS; mount 27/27 PASS; i18n 19/19 PASS.
- Architecture: 27 baseline violations (23 size, 9 flat folder? exact log), no P1-B new file violation; `offer-workspace.tsx` 798 lines.
- Impeccable detector `--changed` returns `[]`; no screenshots.
- OpenSpec strict exit 0 with existing warnings.
- Logs: release-{typecheck,build,node,mount,i18n,architecture}.log, release-impeccable-detect.json.

## Remaining integration checks (P1-C/architect)
- Real Host connected admin/seller create/edit, offered quantity, grid/history and three-locale provider at 360/768/1440 with actual DB registry.
- Full 42+10 connected acceptance and independent AQ/visual audit remain architect/C-owned.
- No claim of parent PASS or owner UAT.
