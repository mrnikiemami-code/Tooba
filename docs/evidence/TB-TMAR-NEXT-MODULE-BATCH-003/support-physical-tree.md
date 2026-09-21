# Support physical tree (TB-TMAR-NEXT-MODULE-BATCH-003)

Every handwritten production `.cs`:

| Relative path | Namespace | Responsibility |
|---|---|---|
| `src/backend/Modules/Support/Tooba.Support.Application/Commands/SupportCommands.cs` | `Tooba.Support.Application.Commands` | command/contracts command |
| `src/backend/Modules/Support/Tooba.Support.Application/Models/SupportDtos.cs` | `Tooba.Support.Application.Models` | application model/DTO |
| `src/backend/Modules/Support/Tooba.Support.Application/Ports/ISupportDirectory.cs` | `Tooba.Support.Application.Ports` | application/contracts port |
| `src/backend/Modules/Support/Tooba.Support.Application/Queries/SupportQueries.cs` | `Tooba.Support.Application.Queries` | application query |
| `src/backend/Modules/Support/Tooba.Support.Domain/Aggregates/SupportTicket.cs` | `Tooba.Support.Domain.Aggregates` | domain aggregate |
| `src/backend/Modules/Support/Tooba.Support.Domain/Entities/TicketMessage.cs` | `Tooba.Support.Domain.Entities` | domain entity |
| `src/backend/Modules/Support/Tooba.Support.Domain/ValueObjects/SupportEnums.cs` | `Tooba.Support.Domain.ValueObjects` | domain value object |
| `src/backend/Modules/Support/Tooba.Support.Infrastructure/Adapters/SupportDemoSnapshot.cs` | `Tooba.Support.Infrastructure.Adapters` | infrastructure adapter |
| `src/backend/Modules/Support/Tooba.Support.Infrastructure/DependencyInjection/SupportModule.cs` | `Tooba.Support.Infrastructure.DependencyInjection` | module DI composition |
| `src/backend/Modules/Support/Tooba.Support.Infrastructure/Directories/SupportDirectory.cs` | `Tooba.Support.Infrastructure.Directories` | directory orchestration |
| `src/backend/Modules/Support/Tooba.Support.Infrastructure/Messaging/SupportOutboxRegistration.cs` | `Tooba.Support.Infrastructure.Messaging` | outbox registration |
| `src/backend/Modules/Support/Tooba.Support.Infrastructure/Persistence/SupportDbContext.cs` | `Tooba.Support.Infrastructure.Persistence` | EF persistence |
| `src/backend/Modules/Support/Tooba.Support.Infrastructure/Seeds/SupportDevelopmentSeed.cs` | `Tooba.Support.Infrastructure.Seeds` | development seed |
| `src/backend/Modules/Support/Tooba.Support.Tests/Architecture/SupportArchitectureGuardTests.cs` | `Tooba.Support.Tests.Architecture` | module source |
| `src/backend/Modules/Support/Tooba.Support.Tests/Behavior/SupportBehaviorCharacterizationTests.cs` | `Tooba.Support.Tests.Behavior` | module source |

Root production `.cs` dump: **0**.
