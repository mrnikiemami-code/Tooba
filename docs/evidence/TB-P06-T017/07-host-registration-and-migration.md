# 07 — Host registration and migration (TB-P06-T017)

## Module boundary

| Piece | Location |
|---|---|
| Domain | `Modules/Story/Tooba.Story.Domain` |
| Application | `Modules/Story/Tooba.Story.Application` (`IStoryDirectory`, DTOs) |
| Infrastructure | `Modules/Story/Tooba.Story.Infrastructure` (`StoryModule`, `StoryDirectory`, `StoryDbContext`) |
| Schema | `story` (`StoryDbContext.Schema`) |
| Migration | `20260827070104_InitialStory` |
| Migration registry | `Tooba.MigrationRunner/ModuleMigrationRegistry` → Descriptor Story / `story` |
| Host composition | `ToobaModuleComposition` → `new StoryModule()` |
| HTTP | `Program.cs` → `app.MapStoryEndpoints()` |
| Composer | `Tooba.Host/Story/StoryPanelComposer.cs` |
| Dev migrate+seed | Marketplace + ProductWorkspace bootstraps |

## Tables (InitialStory)

- `story.Stories` (+ outbox mapping as for other modules)
- `story.StoryItems`

## Solution

Projects added to `Tooba.slnx`; Host csproj references Story Application/Infrastructure.

## Outbox

`StoryOutboxRegistration` registers schema/table; foundation does not publish integration events yet (`Translate` → null).
