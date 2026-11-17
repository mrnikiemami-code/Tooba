# TB-276 — Connected review intent (Scope A) and reviewed failed-refund retry (Scope B)

Line numbers refer to the final tree. Nothing below was inferred from a passing test name: the source was read, then the
focused suites were executed once after the final source change.

---

## A. One exact, durable, fenced review command

### A1. The immutable target and the frozen bytes

`src/frontend/app/returns/return-review-intent.ts` (TB-276 `v2` envelope) stores, before the first possible send:

| Field | Why it exists |
| --- | --- |
| `version = "v2"` | an older/unknown shape can never be read as sendable evidence |
| `action` | closed union `receipt | approve | reject | resume_refund | retry_refund | settle_physical` |
| `target` | closed union `returns | order-operation` — WHICH owning authority executes it |
| `returnRequestId` | the immutable case |
| `routeTargetId` | the frozen owning checkout for `order-operation`, `null` for Returns-owned routes |
| `occurrenceKey` | the reviewed occurrence identity |
| `requestBody` | the EXACT serialized transport body |
| `recordedAt` | audit stamp only; never used to decide anything |

`buildReturnReviewIntent` (`return-review-intent.ts` L104–128) returns `null` — i.e. refuses to create anything — when
the case, occurrence, action, target or body object is missing, when the body is an array or empty `{}`, or when the
target is `order-operation` without a checkout / a Returns-owned route WITH one. So an unaddressable intent is refused
before storage and before any request.

The transport never rebuilds a body from the live snapshot:

```startLine:964:1001:src/frontend/app/admin/admin-return-workspace.tsx
async function dispatch(intent: ReturnReviewIntent): Promise<ReviewAnswer> {
  const body = parseReturnReviewBody(intent);
  if (!body) return { state: "unknown", status: 0 };
  switch (intent.action) {
    case "receipt":
    case "resume_refund":
    case "settle_physical":
      // The frozen reviewed bytes are the ONLY body sent: nothing is rebuilt from a newer detail read.
      return classifyReviewAnswer(
        await sendReviewedReturnCommand(intent.returnRequestId, intent.action === "receipt" ? "receipt" : "recover", body),
      );
    case "retry_refund":
      return classifyReviewAnswer(
        await adminRetryReturnRefund(
          intent.returnRequestId,
          (body.expectedFailedAttemptId as string | null) ?? null,
        ),
      );
    case "approve":
    case "reject": {
      // TB-276: the owning checkout is the FROZEN route identity of the reviewed intent, never the newer snapshot.
      if (!intent.routeTargetId) return { state: "unknown", status: 0 };
      return classifyReviewAnswer(
        await executeAdminOrderOperation(intent.routeTargetId, {
          code: String(body.code ?? ""),
          returnRequestId: intent.returnRequestId,
          reason: (body.reason as string | null) ?? null,
          idempotencyKey: String(body.idempotencyKey ?? intent.occurrenceKey),
        }),
      );
    }
    default:
      return { state: "unknown", status: 0 };
  }
}
```

`executeAdminOrderOperation` is addressed by the **frozen** `routeTargetId`, not `snapshot.checkoutId`. At review time the
workspace captures that checkout once and thereafter never re-derives it, so a case whose payload changes between review
and send cannot silently retarget the approve/reject to a different owning order.

### A2. Verified read-back BEFORE the executor

```startLine:288:310:src/frontend/app/admin/admin-return-workspace.tsx
      const started = epoch.current;
      inFlight.current = true;
      try {
        // Durable (verified read-back) BEFORE the first possible send.
        if (!storeReturnReviewIntent(storage, intent)) {
          setNotice({
            title: "اقدام انجام نشد",
            text: "ذخیرهٔ محلی این دستگاه در دسترس نیست؛ برای جلوگیری از تکرار ناخواسته، اقدام ارسال نشد.",
          });
          return;
        }
        if (!isCurrent(started)) return;
        setStored({ kind: "pending", intent });
        setPending(intent.action);
        const answer = await dispatch(intent);
        if (!isCurrent(started)) return;
        await settle(intent, answer, started);
      } finally {
        inFlight.current = false;
        // A newer surface owns its own pending lock: an old send may never release it.
        if (isCurrent(started)) setPending(null);
      }
```

`storeReturnReviewIntent` (`return-review-intent.ts` L208–220) writes and then compares `getItem(key)` to the exact
serialized bytes; a silent-discarding or throwing storage yields `false`, and the caller returns **before** `dispatch`.
The row is therefore unresolved-before-the-executor, not unresolved-after-it: a process kill between the write and the
response still leaves the identical envelope.

### A3. A pending envelope may never be replaced, and never silently discarded

`send` reads storage first (L259–286): `unavailable` and `unreadable` refuse; a `pending` envelope that is not the exact
same command refuses with «یک اقدام ناتمام برای این پرونده ثبت شده است». The unconditional `finish`-clear of TB-275 is
gone. The reader validates on every read (`decodeIntent` L146–183): version, case, known action, known target, non-empty
occurrence, non-empty body, the target/checkout rule, and a byte-for-byte `JSON.stringify(parsedBody) === requestBody`
check — anything else is `unreadable` evidence, not "nothing stored".

The unreadable envelope is shown and only removable by an explicit Persian action
(`discardUnreadableReturnReviewIntent`, L256–271 → `discardUnreadable`), which re-decodes and refuses to delete VALID
evidence.

### A4. Which refusals prove no effect, and which do not

`PROVEN_NO_EFFECT_REFUSAL_CODES` (`return-review-intent.ts` L286–335) is a literal token list; `isProvenNoEffectRefusal`
matches it exactly. Deliberately excluded and documented in place: transport failures (no code at all),
`return.rejected` (generic client fallback), and `return.recovery.gateway_unavailable` (the pending attempt survives and
must be resumed by the SAME intent). Only a listed code converts a refusal into "release this command and offer a
deliberate new review"; everything else becomes `{state:"unknown"}`.

`classifyReviewAnswer` (`admin-return-workspace.tsx` L946–961) is the only place that decides this, so no message
matching, no `4xx` sweep, no optimistic success. A `200` without a body is `unknown`; a malformed payload is never
`applied`.

### A5. Compare-and-clear on the exact command

`clearReturnReviewIntentIfCurrent` (`return-review-intent.ts` L236–251) requires `getItem(key)` to equal
`serializeReturnReviewIntent(intent)` — the whole envelope — before `removeItem`, and returns `false` if anything else is
stored. `sameReturnReviewIntent` compares case + target + checkout + action + occurrence + exact body. A late answer for
an old intent therefore cannot erase a newer envelope.

### A6. Epoch fencing

One `epoch` ref (L141) with `isCurrent` (L155). It is bumped on mount/retarget (L173) and in the effect cleanup
(L179–182). Every write path is fenced: `loadAt` (detail read, L162–168), `settle` entry (L213), the `applied`/`refused`
branch's `loadAt`, the `send` post-write check (L299), the post-dispatch check (L305), the `finally` pending release
(L308), and the receipt/decision review builders capture `epoch.current` for their follow-up reads. A stale detail read
can neither repaint the newer case nor clear its pending lock, and an old command's storage reconciliation does not
touch the newer surface.

### A7. Remaining Order return menus

Audited: `/admin/orders` navigation to this owner remains (a link, not a second action owner). The TB-275 legacy status
card and its unguarded `RefundFailed` retry button are gone; `admin-screens.tsx` keeps only a compatibility re-export of
`AdminReturnWorkspace`. No duplicate blind approve/retry form survives: approve/reject travel through the existing Order
operations authority via the frozen `routeTargetId`, and retry/refund/physical travel through the Returns-owned routes.

---

## B. Reviewed failed-refund retry replays its deterministic child

### B1. The identity is required at every boundary

Wire (`ReturnAdminEndpoints.cs` L53–67) accepts `expectedFailedAttemptId` as a route/query value and passes it into the
command; it is never a client-authored payload. The owning directory refuses absence:

```startLine:465:495:src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.cs
    private static RefundAttempt RequireReviewedFailedAttempt(ReturnRequest request, Guid? expectedFailedAttemptId)
    {
        if (expectedFailedAttemptId is not { } expected || expected == Guid.Empty)
        {
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryAttemptMismatch);
        }

        var reviewed = request.RefundAttempts
            .FirstOrDefault(x => x.RefundAttemptId == expected)
            ?? throw new ContractOperationException(ReturnsErrorCodes.RecoveryAttemptMismatch);

        if (reviewed.Status != RefundAttemptStatus.Failed || !IsBoundToRequest(request, reviewed))
        {
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryAttemptMismatch);
        }

        return reviewed;
    }
```

`null`, `Guid.Empty`, a foreign id, a non-`Failed` row and a row that is not positively bound to this case's payment id /
amount / currency all give the same typed refusal with zero writes. There is no "current/latest failure" fallback for an
unreviewed caller. The legacy adapter path (`ReturnAdminOperationsAdapter`) answers the stored current failure or
`return.recovery.attempt_unbound`; it does not mint.

### B2. The deterministic child is located BEFORE the new-attempt gates

```startLine:398:436:src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.cs
        // TB-276 (B): the reviewed DEFINITE failure is REQUIRED — every reachable retry boundary must name the stored
        // attempt the operator reviewed, because "the current failure" is a guess that would let an unreviewed caller
        // retry whichever row happened to be newest. The child identity is derived from that exact stored failure, so a
        // duplicate/lost-response POST resolves to the SAME child instead of minting a count-based one.
        var failed = RequireReviewedFailedAttempt(request, command.ExpectedFailedAttemptId);
        var paymentId = request.PaymentId
            ?? throw new ContractOperationException(ReturnsErrorCodes.RefundPaymentMissing);
        var childAttemptId = DeriveRetryAttemptId(request.ReturnRequestId, failed.RefundAttemptId);
        var childKey = DeriveRetryIdempotencyKey(failed.RefundAttemptId);

        var alreadyMinted = request.RefundAttempts
            .FirstOrDefault(x => x.RefundAttemptId == childAttemptId);
        if (alreadyMinted is not null)
        {
            // TB-276 (B): a replay of the PARENT intent answers its deterministic CHILD — whatever that child's stored
            // outcome is (pending, succeeded or failed). Nothing is re-minted, no new key is minted, and a terminal
            // child is never re-sent; the answer is the child's own durable row.
            EnsureRetryAttemptIdentity(request, alreadyMinted, paymentId, childKey);
            await ExecuteRefundAsync(request, alreadyMinted, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return await MapSnapshotAsync(request, cancellationToken);
        }

        // A NEW child only exists for a case that is currently a definite failure of its single reviewable attempt.
        if (request.Status != ReturnRequestStatus.RefundFailed)
        {
            throw new ContractOperationException(ReturnsErrorCodes.RefundRetryInvalidState);
        }

        EnsureReviewedFailureIsTheCurrentOne(request, failed);
```

The order is exactly the one the review demanded: identity → binding → deterministic child → (only if absent) the
`RefundFailed` status gate and the single-reviewable-leaf gate. TB-275's ordering (status and current-leaf gates first,
child lookup at L412) is what made an exact old intent refuse after the child progressed; that is repaired here.

`EnsureRetryAttemptIdentity` (L514–526) pins the stored child to this case's payment, refund amount, trimmed currency and
the derived key; a child that fails any of them is `return.recovery.attempt_mismatch` and is never re-sent.

`EnsureReviewedFailureIsTheCurrentOne` (L486–508) still requires exactly ONE positively bound, not-yet-retried failed leaf
before a NEW child: zero or two reviewable leaves is `return.recovery.attempt_unbound`, and a different leaf than the
reviewed one is `return.recovery.attempt_mismatch`. So a stale/foreign/ambiguous parent mints nothing and a
pending/unknown parent is never treated as a definite failure.

`ExecuteRefundAsync` short-circuits any non-`Pending` attempt (the 273 authority), so a stored `Succeeded` child answers
from storage without a second gateway call, and a stored `Failed` child answers its own failure without a grandchild. The
deterministic derivation itself is `ReturnRetryIdentity.DeriveAttemptId` / `DeriveIdempotencyKey`
(`SHA256("tb275-refund-retry:{case}:{failed}")` and `refund-retry-{failed}`), so two distinct reviewed intents —
parent-vs-child — remain distinguishable while a duplicate of the same intent collapses into one primary key.

### B3. Collision handling stays narrow

The save is wrapped only for a real PostgreSQL `23505` on `returns.refund_attempts` naming
`pk_refund_attempts` or `ix_refund_attempts_IdempotencyKey` (`IsRetryAttemptKeyConflict`, L551–557). The losing entry is
detached individually (`_db.Entry(attempt).State = EntityState.Detached`) — no global tracker clear — and the winner is
re-read through the same `EnsureRetryAttemptIdentity` + `ExecuteRefundAsync` rule.

### B4. Focused proof

`ReturnsOperatorWorkflow275Tests` — final run `15/15`, including the cases this scope owns:

| Test | What it now proves |
| --- | --- |
| `Retry_requires_an_explicit_reviewed_failure_at_every_boundary` | absent/empty/foreign id → typed refusal, no attempt |
| `Retry_refuses_an_identity_that_is_not_a_stored_failure_of_this_case` | a live row of another case cannot be borrowed |
| `Retry_refuses_a_reviewed_attempt_that_is_not_the_stored_failure` | mismatch refusal |
| `Retry_binds_the_child_attempt_to_the_reviewed_failure_deterministically` | child id/key derived from the reviewed failure |
| `A_duplicate_post_answers_the_recorded_child_without_sending_money_again` | **replay**, not the TB-275 `RefundRetryInvalidState` refusal |
| `After_the_reviewed_retry_fails_again_the_child_is_the_only_reviewable_failure` | parent replay still answers the child; a deliberate child review owns the next step |
| `Retry_is_refused_when_the_case_has_no_single_reviewable_failure` | ambiguity mints nothing |
| `Admin_detail_adds_each_line_product_and_unit_without_touching_the_stored_facts` | the detail enrichment is read-only over stored facts |

Mounted workspace suite `admin-return-workspace-276.mount.test.ts` — `10/10`, over the real `AdminReturnWorkspace`:

| Test | Scope |
| --- | --- |
| `the reviewed receipt is durable BY READ-BACK before the first request, and the wire body is that frozen body` | A2 |
| `one click burst produces exactly one command` | A2 / same-tick serialization |
| `a 5xx/unknown answer keeps the exact envelope, and the retry re-sends the identical bytes` | A3 |
| `a proven no-effect refusal releases the command and tells the operator in Persian` | A4 |
| `a fresh mount recovers the retained envelope from storage and sends nothing by itself` | A3 restore-on-remount |
| `a late answer for the OLD case never paints on, or releases, the NEW case surface` | A6 |
| `a malformed success payload is never treated as an applied command` | A4 |
| `a throwing storage getter refuses to send and never crashes the render` | A2 / render safety |
| `a storage that throws on write refuses to send` | A2 |
| `the unresolved card never disappears on a failed or missing fresh read` | A3 |

Queue/mapping/label suite `admin-returns-work-queue.test.ts` — `26/26`; error map `8/8`; `tsc --noEmit` `0`;
`npm run build` `0` (`BUILD_ID gC-lsQo8rzo30IeWT6MwF`); backend `dotnet build Tooba.slnx -c Release` `0` errors; Host
publish `0` errors; module guard `ReturnsModuleAmsc001*` `34/34`.

### B5. Honest limits

- The mounted suite runs in the repository's jsdom mount runner with controlled HTTP; it is not a real browser session and
  it performs no money/stock action.
- No committed two-writer race is claimed: the collision branch is exercised with controlled failures plus the real unique
  index shape, exactly as 275 documented.
- No real PSP/Wallet/Inventory effect was produced anywhere in this task; the only live business write is the single
  review case described in `user-review-case-proof.md`.
