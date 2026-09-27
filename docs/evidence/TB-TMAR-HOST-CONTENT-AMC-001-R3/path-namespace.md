# Path ↔ namespace — Content

Every production .cs under the five Content projects declares a namespace equal to `projectName` + relative folder path (GlobalUsings excluded by design).

Examples:
- Domain/Aggregates/ContentArticle.cs → Tooba.Content.Domain.Aggregates
- Domain/Rules/ContentArticleLifecycleRules.cs → Tooba.Content.Domain.Rules
- Application/Composition/ContentOperation.cs → Tooba.Content.Application.Composition
- Infrastructure/Directories/ContentDirectory.cs → Tooba.Content.Infrastructure.Directories
- Infrastructure/Development/ContentDevelopmentSeed.cs → Tooba.Content.Infrastructure.Development
- Infrastructure/ContentModule.cs → Tooba.Content.Infrastructure
- Endpoints/Admin/ContentEndpoints.cs → Tooba.Content.Endpoints.Admin

No namespace-alias workaround.
