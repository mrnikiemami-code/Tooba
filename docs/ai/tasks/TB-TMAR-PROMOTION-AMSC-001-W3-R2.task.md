# Persisted claim artifact — TB-TMAR-PROMOTION-AMSC-001-W3-R2

Persisted claim artifact — full Architect body received via Bridge `GET /api/tasks/next?channelId=tooba-main` at claim row `4e01c1e9-e582-461b-b1bd-5e0c048be07a` (createdAtUtc 2026-10-08T16:38:15.9933541Z).

- Parent-Task: `TB-TMAR-PROMOTION-AMSC-001-W3-R1`
- Skill: `tooba-architecture-migrate`
- Mode: `BOUNDED_VALIDATOR_CLASSIFICATION_REPAIR`
- Starting HEAD (required): `85d9818d79fa0b1b906c52af16b1a33f1c2149ce`
- Channel: tooba-main, WorkerId: tooba-worker-01, AgentType: cursor

## Authorities

- W0 Analyze: `d00cf666b58ede6c950bdfdd12f95bfd8a0335b3`
- W1 Migrate: `068589331283aac32c25da62b24b372ac1e61331`
- W2 Structure: `597147ea8cc3fa4a0f9d6e301133a7237fc68e43`
- W3 historical Certify: `6bf747455b0ab0d8468595ecf42fb73a4d15a52b` (CERTIFICATION SUPERSEDED / BLOCKED BY W3-R1)
- W3-R1 independent review: `85d9818d79fa0b1b906c52af16b1a33f1c2149ce` (accepted as review evidence, not certification)

## Defect (verified)

`MerchandisingCampaignAdminEndpoints.ListTypesAsync` binds a user-supplied `string? locale` and dispatches
`new ListMerchandisingCampaignTypesQuery(locale)`; the request passes it to
`IMerchandisingCampaignAdminComposer.ListTypesAsync`. No request validator covered it, so the W3 claim
`EXHAUSTIVE_18_VALIDATOR_REQUIRED_3_NO_VALIDATOR_REQUIRED` is false. The correct expected classification is
`19 VALIDATOR_REQUIRED / 2 NO_VALIDATOR_REQUIRED`, subject to verifying every request from disk. Existing
locale-validation precedent: `ListMerchandisingCampaignsQueryValidator` and
`GetMerchandisingCampaignQueryValidator` use `BeWellFormedLocale` + `PromotionValidationCodes.LocaleInvalid`.

## Goal

Make one behavior-preserving transport-shape validation repair for `ListMerchandisingCampaignTypesQuery`
and correct all Promotion-local false W3 certification assertions without prematurely re-certifying.
Preserve routes, DTOs, query behavior for valid/empty locale, error-code ownership, schema, architecture and
Host closure.

## Required actions

- Verify all 21 endpoint-to-`IRequest` dispatches and their validator classification, documenting
  19 REQUIRED + 2 NO_VALIDATOR_REQUIRED with evidence; independently validate the two exemptions.
- Add exactly one FluentValidation validator for `ListMerchandisingCampaignTypesQuery` in the canonical
  Promotion Application validation home, using the existing `PromotionValidationCodes.LocaleInvalid` and
  identical locale-shape semantics. No new code, descriptor, locale parser, normalization or business rule.
  Verify validator DI discovery and pipeline execution; malformed client locale must fail via the canonical
  stable-code/`ApiResponseFactory` path before composer invocation; missing/empty locale preserves behavior.
- Add focused behavior tests for a malformed locale, valid locale and null/blank locale, and for correct
  canonical failure mapping. Ensure tests would fail before the repair.
- Fix the existing Promotion W3 certification guard and relevant W1/W2 guard inventories to check the
  actual 21 distinct endpoint-reachable request types and exact coverage by 19 validators plus 2 explicitly
  justified exemptions (not aggregate arithmetic). No weakening; no obsolete frozen counts.
- Reconcile historical W3 claims honestly: flag historical W3 certification as
  BLOCKED/SUPERSEDED_BY_W3_R1 until a separate fresh Certify wave. Correct 18+3 text to 19+2 where
  recording the current verified matrix, without claiming final W3-R2 certification or altering the global
  Host checkpoint. Append a concise Master Recovery correction and R2 evidence. Preserve historical W3 and
  W3-R1 evidence as immutable records.
- Record the R2 implementation SHA only after commit; do not fabricate a self-referential SoT SHA. Keep
  exactly one Promotion manifest entry; preserve project/root allowlists unless proven required.

## Allowed

Promotion Application validator source and focused Promotion tests; Promotion W1/W2/W3 architecture guards
only where necessary to correct the validator truth; Promotion-specific manifest `certificationNote` and
Promotion-specific SoT status/matrix only; concise Master Recovery appended correction; new R2 evidence and
the exact received `.task.md` under `docs/ai/tasks`.

## Forbidden

Endpoint routes/verbs/signatures or non-validation business behavior changes; new error/localization code,
alternative locale semantics or new dependency; new schema/migration, structural relocation, project graph
changes; unrelated module, Host runtime and frontend changes; altering repository-global Host locks,
accepted checkpoints or unrelated certified modules; weakening guards, widening baselines, hiding Host
failures; claiming COMPLETE_REFERENCE_PATTERN fresh Certify in this repair task; starting the next module or
new task.

## Outcome

`READY_FOR_FRESH_CERTIFY` (NOT CERTIFIED). One validator added; matrix corrected to 19+2; historical W3
verdict flagged `BLOCKED_SUPERSEDED_BY_W3_R1_PENDING_FRESH_CERTIFY`; focused tests green; the 3 remaining
failures are pre-existing and unrelated (reproduced on a clean `85d9818d` worktree).

## Result contract

Per Bridge Task body — `BEGIN_TOOBA_WORKER_RESULT … END_TOOBA_WORKER_RESULT` posted to `POST /api/results`
only after validate + commit + push + `HEAD == origin/main`, then complete the Bridge task lifecycle and
stop.

## Stop rule

STOP after one bounded repair, focused checks, commit, push and the complete Bridge result. Worker PASS does
not mean Architect ACCEPT. No next wave is authorized.

END_TOOBA_TASK
