# Exception logging orchestration

`IExceptionPresentationService` maps once, logs once (Warning business / Error unexpected with exception), writes ProblemDetails via `IProblemDetailsService`.
`ToobaExceptionHandler` is thin delegate.
