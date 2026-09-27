# CQRS request matrix

Endpoint-reachable requests: 51
MediatR 12.5 via AddToobaCqrsFoundation(Content.Application)
All business use cases: Endpoint → ISender → IRequest<Result|Result<T>> → Handler → Ports
