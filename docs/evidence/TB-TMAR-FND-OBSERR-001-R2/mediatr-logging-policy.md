# MediatR logging policy — TB-TMAR-FND-OBSERR-001-R2

## Behaviors (outer → inner)

1. `TracingBehavior<,>` — Activity span; Error status + `exception.type` tag only
2. `ValidationBehavior<,>` — FluentValidation
3. `LoggingBehavior<,>` — Debug lifecycle only

## Decision

| Failure class | LoggingBehavior | Global exception / ApiResponseFactory |
| --- | --- | --- |
| SemanticException | Debug (expected) | Owns client ProblemDetails |
| ValidationException | Debug (expected) | Owns client ProblemDetails |
| PlatformHttpException | Debug (expected) | Owns client ProblemDetails |
| Unexpected | Debug once (type only in message template) | Owns Warning/Error presentation |

## Anti-noise

- No Warning on expected business failures (removes double-log with Host exception pipeline)
- TracingBehavior does **not** log exceptions (status/tags only)
- No request payload in logs or Activity tags
