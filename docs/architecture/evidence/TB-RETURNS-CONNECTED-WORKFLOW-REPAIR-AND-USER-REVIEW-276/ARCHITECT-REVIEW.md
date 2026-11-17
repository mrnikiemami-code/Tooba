# Architect/user checkpoint — TB-276 — 2026-11-17
Status: PARTIAL / USER CHANGES REQUIRED; no final user functional or visual acceptance.
Reviewed source: be47ee81ae0bf2b7e2678752741837ca60af0b70. Source inspection only by Architect; worker runtime/test receipts remain worker evidence.

The authorized case exists per worker and was subsequently exercised by the user. User screenshots show receiving form expanded at page bottom, a free quantity field, and after user approval a Completed case with a successful refund attempt. Latest user state supersedes276's Requested/no receipts/no attempts report. Do not rewrite history or reset the case.

Source-confirmed:
- admin-return-workspace.tsx uses pending==receipt/approve/reject inline sections, not the shared Dialog. Receipt quantity is a text input with inputMode=decimal, no authoritative min/max/step or local remaining-ceiling check. It labels the form row as “قلم1” rather than prominently displaying the actual product and unit.
- Shared Dialog exists in design-system/primitives/overlays.tsx and is used by admin-shipment-plan-modal.tsx. Workspace imports shared MessageBox/ToastRegion, so the precise finding is inconsistent/nonaccepted outcome presentation, NOT absence of a shared-component import. User requires the application's standard notification behavior and no custom page-local substitute.
- ReturnDirectory.ApproveAsync171–281 explicitly calls MarkRefundProcessing, BeginRefundAttempt and ExecuteRefundAsync. Thus approval currently causes money execution. User now explicitly requires a separate financial action; this is a backend behavior change, not a relabel of the combined operation.
- Existing Approved state can represent approved/no-money-started; current ReturnRecoveryPolicy reads it together with RefundProcessing and treats no attempt as unbound. ReturnWorkPolicy has no CanStartRefund action. Both read/action authorities need an explicit coherent awaiting-first-refund description.
- Existing273 exact-attempt recovery and276 deterministic failed-child identity must remain distinct from first execution. Preserve their evidence gates/uncertain-key protocols. The276 no-effect list still includes authorization loss; that is insufficient proof about an earlier send and cannot clear an unresolved financial command.

User directive is recorded in standing AGENTS rules. Only TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277 next, combining UX and actual financial-boundary implementation. No task solely for a fixture and no new case/receipt/approval/refund to manufacture a walkthrough. Existing exercised case and prior audit history remain read-only. Use controlled mounted/owning tests for new stages and real served GET-only UI inspection. No universal provider/financial acceptance.
