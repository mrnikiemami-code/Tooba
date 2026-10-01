# Failure semantics
Domain OperatorProfile Create/Update throw SemanticException(ProfileRejected).
Directory EnsureActor throws SemanticException(ProfileRejected).
Directory UpsertAsync has ZERO catch (InvalidOperationException).
Unknown InvalidOperationException from EF/runtime propagates unchanged.
