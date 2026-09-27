# Primary invariant — W13

Preserved in `ProductMediaDirectory.EnforcePrimaryUniqueness`:

- When media.Count > 0 → exactly one IsPrimary
- First attach → isPrimary=true
- SetPrimary → exclusive flag on target
- Reorder → does not intentionally change primary; uniqueness repair if needed
- Detach primary → remaining repaired to first DisplayOrder/ReferenceId

Covered by ProductMediaGalleryTests (Docker/Testcontainers; skipped when unavailable) and durable architecture guard asserting EnforcePrimaryUniqueness presence.
