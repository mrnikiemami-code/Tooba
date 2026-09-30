# Migrate — Host/Story AMC-001-R1

## Disposition

| Item | Before | After |
| --- | --- | --- |
| Admin list/grid reviewStatus | Domain enum bound in Endpoints | `string?` transport; parse in Application |
| StoryHttpErrors | message.Contains Persian/English branches + Results.* | `ApiResponseFactory.FromSemanticException` / `FromPlatformException` |
| Endpoints → Domain | Present | ZERO (csproj + sources) |
| Composer Guard | InvalidOperation → message HTTP | InvalidOperation → SemanticException via StoryFailureMapper |
| Host/Story | ABSENT | ABSENT (unchanged) |

## Behavior parity

- Stable codes preserved: story.missing / cta.rejected / mutation.rejected / tenant.missing / reviewStatus.invalid
- Routes/verbs unchanged
