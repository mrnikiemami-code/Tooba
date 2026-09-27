# Validation matrix — W13 ProductMedia

| Request | Classification | Rules |
|---|---|---|
| GetProductMediaQuery | NO_VALIDATOR_REQUIRED | route Guid only |
| GetProductMediaReadinessQuery | NO_VALIDATOR_REQUIRED | route Guid only |
| AttachProductMediaCommand | VALIDATOR_REQUIRED | MediaAssetId NotEmpty → workspace.media.asset.missing |
| AttachPlaceholderProductMediaCommand | NO_VALIDATOR_REQUIRED | optional AltText |
| ReorderProductMediaCommand | VALIDATOR_REQUIRED | OrderedMediaAssetIds NotNull → catalog.validation.product_media_ordered_ids_required |
| SetPrimaryProductMediaCommand | NO_VALIDATOR_REQUIRED | route ids |
| PatchProductMediaCommand | NO_VALIDATOR_REQUIRED | AltText unrestricted |
| DetachProductMediaCommand | NO_VALIDATOR_REQUIRED | route ids |

Exact-set / product existence / membership remain in ProductMediaDirectory (not FluentValidation).
