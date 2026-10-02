# Story AMC W1 migrate — semantic/localization

Parent: TB-TMAR-STORY-AMC-001 (Analyze)
Task: TB-TMAR-STORY-AMC-001-W1

## Changes

- Domain/Infrastructure business failures now `SemanticException(SemanticError(StoryErrorCodes.*))`
- Removed `StoryFailureMapper.ToSemantic` message-text classification
- `StoryPresentationComposer.Guard` no longer remaps by exception message
- `RequireTenantId` throws `story.tenant.missing` directly
- Domain + Infrastructure reference `Tooba.Story.Contracts` for stable codes
- `StoryFoundationTests` expect `SemanticException` for business failures

## Behavior

Same HTTP/status mapping via existing catalog codes (Missing / CtaRejected / TenantMissing / MutationRejected / ReviewStatusInvalid). No schema change. Seed fixture FA titles unchanged (not error localization).

## Not in this wave

God-file splits, full validator matrix, Result&lt;T&gt; Domain strategy, ARCH-COMPLETE-002 structure cert.
