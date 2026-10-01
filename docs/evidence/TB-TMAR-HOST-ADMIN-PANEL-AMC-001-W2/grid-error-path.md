# grid-error-path — W2
PartyAdminSellersGridPolicies.Normalize throws GridQueryValidationException (stable ErrorCode) — no PlatformHttpException(ex.Message) wrap.
QueryAdminSellersGridQueryHandler catches GridQueryValidationException → Result.Failure(SemanticError(ex.ErrorCode)).
No ex.Message classification/presentation on this route.
ApiResponseFactory maps SemanticError; unknown grid.* codes fall back to HTTP 400.
Hard-coded FA messages inside GridQueryValidationException factory remain BuildingBlocks legacy; Party path does not expose them as client contract.
