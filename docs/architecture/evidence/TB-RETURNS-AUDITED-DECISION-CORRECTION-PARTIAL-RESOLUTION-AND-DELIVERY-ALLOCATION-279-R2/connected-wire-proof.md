# TB-279-R2 — CONNECTED WIRE PROOF (B)

The reviewed operator actions now carry ONE explicit shape from the real modal to the owning directory. Nothing is
reconstructed from a fresh GET at dispatch time and nothing falls back to a default.

## Frozen values

| Action | Transport | Frontend source | Owner gate |
| --- | --- | --- | --- |
| first refund (`start_refund`) | route `POST /v1/admin/returns/{id}/start-refund?expectedDecisionVersion=N`, empty body | `return-api.ts:1517-1522`, `return-ui.tsx:259-260` | `ReturnDirectory.cs:225` (`EnsureReviewedPlanVersion` on the loaded case) then `:339` on the locked re-read |
| reviewed failed retry | query `expectedFailedAttemptId`, `expectedDecisionVersion` | `return-api.ts:1407-1414` | `ReturnDirectory.cs:243` after `RequireReviewedFailedAttempt` |
| approve / reject | body `ExpectedDecisionVersion` + `ExpectedResolutionVersion` | `return-api.ts:1313-1334`, seller endpoints `:18/27` | `ReturnDirectory.cs:225/243` |
| publish resolution | body `expectedVersion` + five portions per item | `return-api.ts:1530-1543` | `ReturnDirectory.Resolutions.cs:104-149` |
| custody | body `quantity`, `condition`, `proofReference`, `proofKind` | `return-api.ts:1557-1565` | `ReturnDirectory.Resolutions.cs:252-344` |

## Typed refusals (no defaults)

| Missing / stale value | Code | HTTP |
| --- | --- | --- |
| no reviewed plan revision on a decision over a plan | `return.resolution.review_version_required` | 400 |
| stale decision or plan revision | `return.correction.version_stale` | 409 |
| portion off the owning unit quantum | `return.resolution.portion_invalid` | 400 |
| custody off the quantum / over the condition ceiling | `return.custody.quantity_invalid` | 400 |
| unproven carrier handback | `return.custody.proof_kind_unavailable` | 400 |

`ResolutionReviewVersionRequired` is registered once in `ReturnsErrorCatalogContributor.cs:98`, declared in
`ReturnsErrorCodes.cs:559`, localized in both resx files, and covered by the AMSC guard counts (87 HTTP-reachable
codes + 1 platform fault). The hosted guard suite passes 34/34.

## Mounted facts that prove the connection (jsdom, real components)

- "all three TB-279 reviews open ONE dialog and send nothing until confirmed"
- "the correction sends the reviewed reason and the exact expected decision version"
- "the resolution dialog refuses an over-received split locally and sends one frozen body when lawful"
- "custody needs a real proof reference and stays inside the rejected remainder"

These assert the exact request bodies/params the owning API helpers produce; the directory-level version refusals are
proven separately (`B_a_decision_over_a_reviewed_plan_must_carry_that_plan_version`).

## Honest limits

- No live active case exists, so no served dialog was opened against real data; the served proof is the read-only case
  page plus the typed refusals.
- The mounted suite proves behaviour, not geometry.
- Money/stock ports are controlled; no provider effect was triggered.
