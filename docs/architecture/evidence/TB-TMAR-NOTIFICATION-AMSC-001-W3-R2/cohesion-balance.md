# TB-TMAR-NOTIFICATION-AMSC-001-W3-R2 — cohesion-balance

File-cohesion audit of the touched surface (Structure skill §12 — prevent both god-files and
cosmetic over-splitting):

- The flatten moved whole files only; no file content was merged, split, or rewritten beyond the
  namespace line and stale self-`using` removal. Each file keeps its single responsibility:
  - `*Command.cs` / `*Query.cs` files carry the MediatR record + its `IRequestHandler` (the module's
    established co-location idiom, unchanged since W1 and inside one production source file —
    counted by file, per skill §8, not by type).
  - `*Validator.cs` files carry one `AbstractValidator<T>` each.
- No new empty or ceremonial folder was created; two stale empty `Validators/` shells inside
  `Customer/` and `Seller/` were deleted.
- No file was split to game a guard; no unrelated file was reorganized.
- Largest touched files remain far below god-file thresholds (the largest, e.g.
  `ListCustomerNotificationsQuery.cs`, ≈ 33 lines).
- The shared cross-capability folders keep their W1/W2 shape: `Composition/`
  (`NotificationOperation` typed-fault seam), `Models/` (`NotificationHttpMapper`, response models,
  `NotificationRecipientKindMapping`), `Ports/` (`INotificationDirectory`), `Rendering/`
  (`NotificationCopy`), `Validators/` (`NotificationValidationCodes`).

File-Cohesion-State = `COHESIVE` (unchanged); no `OVER_SPLIT`, no `MULTI_RESPONSIBILITY_COHESION_VIOLATION`.
