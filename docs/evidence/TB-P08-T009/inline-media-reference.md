# inline-media-reference

Structured DAM refs (featured/SEO/gallery) counted via `CountStructuredReferencesAsync`.
Inline TipTap body refs store `data-media-asset-id` but are NOT in the structured reference index (T005).
Media Host has no delete API today; unassign removes assignments only.
Correct body-reference index needs a content-reference registry — not a fragile HTML regex. Documented; no parser hack added.
