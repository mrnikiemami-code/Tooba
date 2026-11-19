# Gate 0 proof — TB-282

## 0a Full occurrence identity on winner fold

- Source: `StoryDirectory.Occurrences.cs` → `AnswersOccurrenceIdentity(winner, actionCode, intentHash, sellerPartyId, expectedStoryId)`
- Fold refuses a winner that matches hash/key but not shop/action/story target
- Test: `Gate0a_winner_fold_refuses_different_shop_even_with_same_intent_hash` — foreign shop winner interceptor → `StoryErrorCodes.OccurrenceConflict`; zero shop-A story rows

## 0b Detach attempt-owned staged rows

- Source: `DetachAttemptOwned(occurrenceKey, tenantId, attemptStoryId)` — only this attempt's Story/items/occurrence entries; no `ChangeTracker.Clear`
- Test: `Gate0b_losing_create_detaches_staged_rows_so_later_save_is_clean` — losing create then subsequent lawful SaveChanges; winner intact; losing rows absent

## 0c Real two-context optimistic concurrency

- Two contexts preload the same original Story; writer1 saves; writer2 owning command hits SaveChanges with original track; interceptor/branch asserts concurrency; typed loser; no extra occurrence
- Test: Gate0c facts in `StoryMarketplace282Gate0Tests` (InMemory + PG skippable where applicable)
- Clarifies R5 C2: prior suite proved stale-command path; this proves concurrent original tracking

## Result

Focused Gate0 suite **4/4 PASS**. ZERO migration.
