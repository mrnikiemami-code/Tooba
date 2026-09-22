# Support Physical Tree

Handwritten Support production `.cs` (path | namespace | responsibility). Tests omitted from responsibility table but exist under `Tooba.Support.Tests`.

## Domain
| Path | Namespace | Responsibility |
|------|-----------|----------------|
| `.../Domain/Aggregates/SupportTicket.cs` | `Tooba.Support.Domain.Aggregates` | Ticket aggregate |
| `.../Domain/Entities/TicketMessage.cs` | `Tooba.Support.Domain.Entities` | Message entity |
| `.../Domain/ValueObjects/SupportEnums.cs` | `Tooba.Support.Domain.ValueObjects` | Enums |

## Application
| Path | Namespace | Responsibility |
|------|-----------|----------------|
| `.../Ports/ISupportDirectory.cs` | `Tooba.Support.Application.Ports` | Directory port + enum parsing |
| `.../Ports/ISupportDemoPreviewPort.cs` | `Tooba.Support.Application.Ports` | Demo preview seam |
| `.../Models/SupportDtos.cs` | `Tooba.Support.Application.Models` | Snapshot/list DTOs |
| `.../Models/SupportDirectoryInputs.cs` | `Tooba.Support.Application.Models` | Directory input records |
| `.../Models/SupportDemoSnapshotDto.cs` | `Tooba.Support.Application.Models` | Demo snapshot DTO |
| `.../Errors/SupportErrorCodes.cs` | `Tooba.Support.Application.Errors` | Stable public codes |
| `.../Errors/SupportExceptionMapper.cs` | `Tooba.Support.Application.Errors` | Exact-code → SemanticError |
| `.../Commands/*/...Command.cs` (10) | `Tooba.Support.Application.Commands.*` | MediatR commands+handlers |
| `.../Queries/*/...Query.cs` (7) | `Tooba.Support.Application.Queries.*` | MediatR queries+handlers |

## Endpoints
| Path | Namespace | Responsibility |
|------|-----------|----------------|
| `.../SupportEndpointModule.cs` | `Tooba.Support.Endpoints` | Map + presentation DI |
| `.../Customer/ISupportCustomerAuthorizer.cs` | `Tooba.Support.Endpoints.Customer` | Auth seam |
| `.../Customer/SupportCustomerEndpoints.cs` | `Tooba.Support.Endpoints.Customer` | Customer HTTP + wire DTOs |
| `.../Seller/ISupportSellerAuthorizer.cs` | `Tooba.Support.Endpoints.Seller` | Auth seam |
| `.../Seller/SupportSellerEndpoints.cs` | `Tooba.Support.Endpoints.Seller` | Seller HTTP |
| `.../Admin/ISupportAdminAuthorizer.cs` | `Tooba.Support.Endpoints.Admin` | Auth seam |
| `.../Admin/SupportAdminEndpoints.cs` | `Tooba.Support.Endpoints.Admin` | Admin HTTP + patch/demo |
| `.../Errors/SupportErrorCatalogContributor.cs` | `Tooba.Support.Endpoints.Errors` | Error catalog |

## Infrastructure
| Path | Namespace | Responsibility |
|------|-----------|----------------|
| `.../Directories/SupportDirectory.cs` | `Tooba.Support.Infrastructure.Directories` | Directory impl |
| `.../Persistence/SupportDbContext.cs` | `Tooba.Support.Infrastructure.Persistence` | EF context |
| `.../Adapters/SupportDemoSnapshot.cs` | `Tooba.Support.Infrastructure.Adapters` | Demo ids/store/adapter |
| `.../Seeds/SupportDevelopmentSeed.cs` | `Tooba.Support.Infrastructure.Seeds` | Dev seed |
| `.../Messaging/SupportOutboxRegistration.cs` | `Tooba.Support.Infrastructure.Messaging` | Outbox registration |
| `.../DependencyInjection/SupportModule.cs` | `Tooba.Support.Infrastructure.DependencyInjection` | Module DI |

## Host (composition only)
| Path | Namespace | Responsibility |
|------|-----------|----------------|
| `Host/.../Support/SupportDevelopmentSeedHost.cs` | `Tooba.Host.Support` | Dev bootstrap |
| `Host/.../Customer/HostSupportCustomerAuthorizer.cs` | `Tooba.Host.Customer` | Auth adapter |
| `Host/.../Seller/HostSupportSellerAuthorizer.cs` | `Tooba.Host.Seller` | Auth adapter |
| `Host/.../Admin/HostSupportAdminAuthorizer.cs` | `Tooba.Host.Admin` | Auth adapter |

## Verdict
**Support-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE**
