# CORRECTION ADDENDUM — TB-282-R18

Append-only. Prior R17 records stay as written; this addendum corrects the conclusions they implied.

## C1 — R17 "one toast / compact confirm" was presentation, not moderation

R17 delivered a single native toast and a compact count-only confirmation, but its own contract stated two
things that mattered more than the styling:

1. the Story feedback used `toast.info` for a **cannot-run** action, so a refusal looked like an
   informational note;
2. the persistent amber `story-pending-recovery` section stayed on screen for any stored command.

Both are presentation defects, and both are repaired here. Neither explains the owner's approval failure.

## C2 — the real approval failure was an advertised action that the server always refuses

The owner's story `01a04246-b309-7000-b1de-681552e70532` is `Seller + Submitted` with `contentReady=false`:
its only item keeps a legacy `MediaUrl` (`/images/stories/2.jpg`) and no owned `MediaAssetId`.
`StoryDirectory.AdminApproveAsync` runs `EnsureStoredContentAuthorizedAsync` → `HasPlayableContent` before
any review mutation, so the owning command can only answer `story.content.notReady`.
`StoryPublicationPolicy.ProjectActionCodes` nevertheless advertised `approve`, so the UI offered an action
that was impossible by construction; the refusal then collapsed into «۱ مورد انجام نشد».

Correction of scope: the previous rounds treated repeated 403/refusal symptoms as UI/auth issues
(`admin.actor.missing`, dev-context, InMemory projection). Those were real blockers at the time and were
closed in R8/R9; the surviving failure was this readiness/action-code mismatch, and it is fixed at the
source of truth (`ProjectActionCodes`) rather than by weakening the approve gate.

## C3 — "unknown" was overstated for pre-send blocks

`executeAdminOpBatch` counted a blocked entry (in-flight, storage barrier, envelope mismatch) as
`unknown` although nothing had been sent. That produced the warning-coloured "uncertain" toast for a
deterministic non-effect. Pre-send blocks are now `failed` with a plain reason; only genuine
transport/response uncertainty stays `unknown`/`warning`.

## C4 — terminal refusals no longer masquerade as replays

A stored envelope used to survive a definite 4xx, so the next attempt looked like a storage/envelope
conflict instead of the original refusal. Terminal refusals now clear their own envelope
(`isTerminalRefusal`), while loss/unverified-200 stays retained for exact replay and 408/425/429 remain
retryable-uncertain.

## C5 — what this result does NOT claim

- It is **not** owner acceptance, and not Architect ACCEPT.
- It does **not** make the owner's existing legacy record approvable; that record is genuinely
  ineligible and needs the seller to upload the slide again (reject → replace → resubmit).
- The served browser evidence is a controlled read-only measurement on next-dev; no live approve/upload
  was sent, so live moderation remains unproven by this task.
