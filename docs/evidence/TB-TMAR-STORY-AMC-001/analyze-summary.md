# Story module AMC Analyze — TB-TMAR-STORY-AMC-001

Target: `Tooba.Story.*` (module, not Host)
Skills: analyze (this wave) → migrate → certify
Mode: ANALYSIS_ONLY

## Verdict

`FOUNDATION_PARTIAL` — Host evacuation already CLOSED_HOST_ZERO; module HTTP/CQRS shell exists; **not** ARCH-COMPLETE-002 ready.

## Critical blockers (must migrate)

1. **Message-text error classification** — `StoryFailureMapper.ToSemantic` parses Persian/English exception `Message` (`یافت نشد`, `ناامن`, Tenant/resolve) to pick `StoryErrorCodes`. Violates multilingual + semantic-code architecture.
2. **Hardcoded FA user-facing failure text** — Domain `StoryEntities` / Infra `StoryDirectory` throw `InvalidOperationException("…فارسی…")` as transport for business failures.
3. **Presentation Guard depends on (1)** — `StoryPresentationComposer.Guard` catches `InvalidOperationException` and remaps via message text.

## Structural debt (later waves / cert blockers)

| Surface | Finding |
| --- | --- |
| `StoryEntities.cs` | GOD_FILE — enums + tenant ids + rules + aggregate + item |
| `StoryContracts.cs` | GOD_FILE — DTOs + `IStoryDirectory` port dump |
| `StoryDirectory.cs` | Oversized persistence/application adapter |
| `StoryValidators.cs` | Only `TitleRequired`; most commands NO_VALIDATOR / incomplete matrix |
| Handlers | Thin MediatR wrapping composer; return snapshots not `Result<T>` |
| Manifest | Story not `structureCertified` |

## Protected / already good

- Endpoints do not reference Domain (R1 preserved)
- `StoryHttpErrors` uses `ApiResponseFactory` / `SemanticException`
- Error catalog + `StoryErrors.resx` / `.fa.resx` exist for current codes
- Host Story folder ABSENT; `MapStoryModuleEndpoints`

## Recommended waves

- **W1 (this)**: Analyze evidence + SoT analyze stamp
- **W2**: Migrate semantic/localization — Domain/Infra throw `SemanticException(SemanticError)` with stable codes; delete message classification; update focused tests
- **W3**: Certify semantic/localization slice + durable guard; remaining structure debt explicit (not false full STRUCTURE_CERTIFIED)

## Behavior

No production code change in Analyze wave.
