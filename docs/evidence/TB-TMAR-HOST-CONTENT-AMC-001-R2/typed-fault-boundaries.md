# Typed fault boundaries

- Content.Infrastructure directories / grid: ContractOperationException(stableCode)
- Content.Domain expected faults: ContractOperationException(stableCode)
- ContentOperation: catch ContractOperationException → SemanticError(ex.Code) only
- MediaAssetReadinessBridge: ContractOperationException(MediaAssetContractCodes.AssetMissing)
- ContentMediaAssetValidator: when Code==AssetMissing → ContractOperationException(ContentErrorCodes.MediaNotFound)
- Localization EnsureActiveLanguageCodeAsync: ContractOperationException(LanguageErrorCodes.Inactive)
- Endpoints IContentAdminAuthorizer may still use PlatformHttpException (HTTP boundary only)
