# Deterministic race proof

Test Inflight_retirement_does_not_remove_slot_after_new_attachment:
- Hook AfterRefCountZeroBeforeRecheckRemove blocks between zero and recheck
- Concurrent Acquire attaches during window
- Assert same slot retained; RefCount=1; not Retired; dictionary still holds slot
- Parent unconditional TryRemove would fail this test
