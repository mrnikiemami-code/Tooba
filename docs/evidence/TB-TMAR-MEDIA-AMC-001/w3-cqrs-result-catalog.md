# TB-TMAR-MEDIA-AMC-001 — W3 CQRS + Result + error catalog

## Changes
- Application capability `Assets/`: Commands, Queries, Validators + `Composition/MediaOperation`
- Contracts: `MediaErrorCatalogContributor`, `MediaErrorResourceSet`, `Resources/MediaErrors(.fa).resx`
- Endpoints: Admin thin `ISender` + `ApiResponseFactory`; serving via MediatR metadata + `IMediaObjectStore`
- Infra: registers Media error catalog + resource set
- Host: `AddToobaCqrsFoundation(... UploadMediaAssetCommand.Assembly)`

## Validator matrix
| Request | Classification |
| --- | --- |
| UploadMediaAssetCommand | VALIDATOR_REQUIRED_PRESENT |
| QueryMediaAssetsQuery | VALIDATOR_REQUIRED_PRESENT |
| GetMediaAssetQuery | VALIDATOR_REQUIRED_PRESENT |
| GetMediaStorageKeyQuery | NO_VALIDATOR_REQUIRED |

## Coupling
Media → foreign App/Infra/Domain: ZERO (unchanged)

Durable guard: `MediaModuleAmcW3CqrsGuardTests`
