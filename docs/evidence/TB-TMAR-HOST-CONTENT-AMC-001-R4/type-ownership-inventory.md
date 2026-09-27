# Type ownership inventory — TB-TMAR-HOST-CONTENT-AMC-001-R4

## Scope
Every production type under Content.Application Models/Ports/Commands/Queries/Validators before/after R4.

## Classification rule
Ownership by consumer boundary, not filename suffix.

## Summary
| Owner | Count | Notes |
|---|---|---|
| CONTENT_APPLICATION_INTERNAL | all CQRS requests, snapshots, grid/workspace/picker DTOs, directory ports, validators | no foreign module consumers |
| CONTENT_CONTRACTS_BOUNDARY | ContentErrorCodes only (pre-existing) | no additional DTOs/ports required |
| CONTENT_ENDPOINT_TRANSPORT | HTTP body records in Endpoints | unchanged |
| DUPLICATE_LEGACY_SHAPE_REMOVE_OR_REPOINT | Models.*Command shapes | removed; ports accept MediatR commands |

## Models (legacy *Contracts.cs → capability Models)
| Type | Final owner | Target |
|---|---|---|
| PagedResult&lt;T&gt;, PublishedArticleItem, AdminArticleSnapshot, ArticlePreviewSnapshot, ArticleHistory* | APPLICATION_INTERNAL | Articles/Models |
| ArticleCommentAdminDto, ArticleCommentPage | APPLICATION_INTERNAL | Comments/Models |
| ArticleGalleryItemDto, ArticleMediaWorkspaceDto | APPLICATION_INTERNAL | Media/Models |
| ContentAuthor*Dto, PublishedContentAuthorItem | APPLICATION_INTERNAL | Authors/Models |
| ContentCategory*Dto, ReorderContentCategoryItem, PublishedContentCategoryItem | APPLICATION_INTERNAL | Categories/Models |
| ContentTagDto | APPLICATION_INTERNAL | Tags/Models |
| CreateArticleCommand / UpdateArticleCommand (Models) | DUPLICATE_REMOVE | MediatR Articles.Commands |
| CreateArticleCommentCommand / ModerateArticleCommentCommand (Models) | DUPLICATE_REMOVE | MediatR Comments.Commands / Note via command |
| CreateContentAuthorCommand / UpdateContentAuthorCommand | DUPLICATE_REMOVE | MediatR Authors.Commands |
| CreateContentCategory* / Update* / Move* | DUPLICATE_REMOVE | MediatR Categories.Commands |
| CreateContentTagCommand | DUPLICATE_REMOVE | MediatR Tags.Commands |

## Ports
All Application→Infrastructure ports remain APPLICATION_INTERNAL under owning capability Ports/.
IContentMediaAssetValidator → Media/Ports.
No port moved to Contracts (no foreign module consumer).

## Commands / Queries / Validators
Flattened to `&lt;Capability&gt;/{Commands,Queries,Validators}/*.cs` with exact path↔namespace.
