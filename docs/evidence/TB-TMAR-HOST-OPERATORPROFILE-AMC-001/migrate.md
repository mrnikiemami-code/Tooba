# Migrate — Host/OperatorProfile AMC-001

- Added OperatorProfile.Endpoints (Admin endpoints + authorizer port + error catalog)
- Added Application CQRS Get/Upsert + FluentValidation
- Directory maps InvalidOperation domain rejects → SemanticException operator.profile.rejected
- Host HostOperatorProfileAdminAuthorizer thin adapter
- Deleted Host/OperatorProfile
