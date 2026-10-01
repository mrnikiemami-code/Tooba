# cqrs-validator-matrix — CERT

## Party sellers (canonical module CQRS)
Endpoint → ISender → IRequest/Handler → Result → ApiResponseFactory

| Request | Validator classification | Physical validator |
| --- | --- | --- |
| ListAdminSellersQuery | NO_VALIDATOR_REQUIRED | absent (correct) |
| QueryAdminSellersGridQuery | VALIDATOR_REQUIRED_ENVELOPE_ONLY | Admin/Sellers/Validators/QueryAdminSellersGridQueryValidator.cs |

Stable code: party.admin.sellers.validation.grid_request_required

## Host retained
| Surface | CQRS exception | Presentation |
| --- | --- | --- |
| dashboard | HOST_PRESENTATION_COMPOSITION_CQRS_EXCEPTION | Result + ApiResponseFactory + IAdminPanelAccess |
| dev-context | HOST_DEVELOPMENT_PRESENTATION_CQRS_EXCEPTION | Result + SemanticError + ApiResponseFactory |

No fake Host MediatR invented.
