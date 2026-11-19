# Directory and Media proof (R1)

## Public reader (B1)

Fresh write context creates Active + Scheduled playable stories; separate read context (new DbContext / same in-memory name) returns both with non-empty Items at `now`; before scheduled start only Active; after scheduled end only Active.

## Role actions (B5)

After approve+publish, seller get includes `stop` and `return_to_draft`, excludes `approve` / `approve_and_publish`; admin get includes stop-class admin actions without seller return_to_draft.

## Media fail-closed (B2)

`StoryDirectory(db, party, media: null)` seller add-item with asset id → `story.media.unavailable` before mutation.

## Media upload (B3)

Code path requires occurrence, hashes intent, scopes occurrence by tenant+shop, compensates orphan Ready asset on binding save failure. Focused recording-store automated facts not shipped in this PARTIAL.
